using FastGuide.ServiceDefaults;
using FastGuide.Api.Dtos;
using FastGuide.Infrastructure;
using FastGuide.Infrastructure.Data;
using FastGuide.Infrastructure.Ingestion;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.AddServiceDefaults();
builder.Services.AddFastGuideInfrastructure(builder.Configuration);

// Add Swagger/OpenAPI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "FastGuide API", Version = "v1" });
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost")
            .AllowCredentials();
    });
});

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromSeconds(60),
                PermitLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));

    // Add a default policy
    options.AddPolicy("default", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromSeconds(60),
                PermitLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));
});

var app = builder.Build();

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    await next();
});

// Global exception handler middleware
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var logger = exceptionApp.ApplicationServices.GetRequiredService<ILogger<Program>>();
        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        logger.LogError(exception, "Unhandled exception occurred");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new
        {
            message = "An unexpected error occurred",
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(response);
    });
});

// Enable CORS
app.UseCors();

// Enable rate limiting
app.UseRateLimiter();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FastGuide API v1");
        options.RoutePrefix = "api-docs";
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();

// Health check endpoint
app.MapGet("/health", () => TypedResults.Ok(new { status = "healthy" }))
    .WithName("Health")
    .AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FastGuideDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    if (builder.Configuration.GetValue("StartupIngestionEnabled", true))
    {
        // Fast local bootstrapping: perform one ingestion run at startup for MVP freshness.
        var orchestrator = scope.ServiceProvider.GetRequiredService<IngestionOrchestrator>();
        await orchestrator.RunIngestionAsync(CancellationToken.None);
    }
}

app.MapGet("/api/v1/channels", async Task<Ok<IReadOnlyList<ChannelDto>>> (FastGuideDbContext db) =>
{
    var channels = await db.Channels
        .AsNoTracking()
        .OrderBy(c => c.Name)
        .Select(c => new ChannelDto(c.Id, c.Name, c.Description, c.Category))
        .ToListAsync();

    return TypedResults.Ok<IReadOnlyList<ChannelDto>>(channels);
})
    .WithName("GetChannels")
    .Produces<IReadOnlyList<ChannelDto>>(StatusCodes.Status200OK)
    .RequireRateLimiting("default");

app.MapGet("/api/v1/channels/{id:guid}", async Task<Results<Ok<ChannelDto>, NotFound>> (Guid id, FastGuideDbContext db) =>
{
    var channel = await db.Channels.AsNoTracking()
        .Where(c => c.Id == id)
        .Select(c => new ChannelDto(c.Id, c.Name, c.Description, c.Category))
        .FirstOrDefaultAsync();

    return channel is null ? TypedResults.NotFound() : TypedResults.Ok(channel);
})
    .WithName("GetChannelById")
    .Produces<ChannelDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .RequireRateLimiting("default");

app.MapGet("/api/v1/now", async Task<Ok<IReadOnlyList<ProgramSlotDto>>> (FastGuideDbContext db) =>
{
    var now = DateTime.UtcNow;

    var slots = await db.ProgramSlots
        .AsNoTracking()
        .Where(ps => ps.StartTimeUtc <= now && ps.EndTimeUtc > now)
        .Include(ps => ps.Channel)
        .OrderBy(ps => ps.Channel!.Name)
        .Select(ps => new ProgramSlotDto(ps.ChannelId, ps.Channel!.Name, ps.Title, ps.Description, ps.StartTimeUtc, ps.EndTimeUtc))
        .ToListAsync();

    return TypedResults.Ok<IReadOnlyList<ProgramSlotDto>>(slots);
})
    .WithName("GetNowPlaying")
    .Produces<IReadOnlyList<ProgramSlotDto>>(StatusCodes.Status200OK)
    .RequireRateLimiting("default");

app.MapGet("/api/v1/search", async Task<Ok<SearchResultDto>> (string query, FastGuideDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(query))
    {
        return TypedResults.Ok(new SearchResultDto([], []));
    }

    query = query.Trim();

    var channels = await db.Channels
        .AsNoTracking()
        .Where(c => EF.Functions.Like(c.Name, $"%{query}%"))
        .OrderBy(c => c.Name)
        .Select(c => new ChannelDto(c.Id, c.Name, c.Description, c.Category))
        .Take(30)
        .ToListAsync();

    var programs = await db.ProgramSlots
        .AsNoTracking()
        .Where(ps => EF.Functions.Like(ps.Title, $"%{query}%"))
        .Include(ps => ps.Channel)
        .OrderBy(ps => ps.StartTimeUtc)
        .Select(ps => new ProgramSlotDto(ps.ChannelId, ps.Channel!.Name, ps.Title, ps.Description, ps.StartTimeUtc, ps.EndTimeUtc))
        .Take(50)
        .ToListAsync();

    return TypedResults.Ok(new SearchResultDto(channels, programs));
})
    .WithName("Search")
    .Produces<SearchResultDto>(StatusCodes.Status200OK)
    .RequireRateLimiting("default");

// Legacy routes for backward compatibility
app.MapGet("/channels", async Task<Ok<IReadOnlyList<ChannelDto>>> (FastGuideDbContext db) =>
{
    var channels = await db.Channels
        .AsNoTracking()
        .OrderBy(c => c.Name)
        .Select(c => new ChannelDto(c.Id, c.Name, c.Description, c.Category))
        .ToListAsync();

    return TypedResults.Ok<IReadOnlyList<ChannelDto>>(channels);
});

app.MapGet("/channels/{id:guid}", async Task<Results<Ok<ChannelDto>, NotFound>> (Guid id, FastGuideDbContext db) =>
{
    var channel = await db.Channels.AsNoTracking()
        .Where(c => c.Id == id)
        .Select(c => new ChannelDto(c.Id, c.Name, c.Description, c.Category))
        .FirstOrDefaultAsync();

    return channel is null ? TypedResults.NotFound() : TypedResults.Ok(channel);
});

app.MapGet("/now", async Task<Ok<IReadOnlyList<ProgramSlotDto>>> (FastGuideDbContext db) =>
{
    var now = DateTime.UtcNow;

    var slots = await db.ProgramSlots
        .AsNoTracking()
        .Where(ps => ps.StartTimeUtc <= now && ps.EndTimeUtc > now)
        .Include(ps => ps.Channel)
        .OrderBy(ps => ps.Channel!.Name)
        .Select(ps => new ProgramSlotDto(ps.ChannelId, ps.Channel!.Name, ps.Title, ps.Description, ps.StartTimeUtc, ps.EndTimeUtc))
        .ToListAsync();

    return TypedResults.Ok<IReadOnlyList<ProgramSlotDto>>(slots);
});

app.MapGet("/search", async Task<Ok<SearchResultDto>> (string query, FastGuideDbContext db) =>
{
    query = query.Trim();

    var channels = await db.Channels
        .AsNoTracking()
        .Where(c => EF.Functions.Like(c.Name, $"%{query}%"))
        .OrderBy(c => c.Name)
        .Select(c => new ChannelDto(c.Id, c.Name, c.Description, c.Category))
        .Take(30)
        .ToListAsync();

    var programs = await db.ProgramSlots
        .AsNoTracking()
        .Where(ps => EF.Functions.Like(ps.Title, $"%{query}%"))
        .Include(ps => ps.Channel)
        .OrderBy(ps => ps.StartTimeUtc)
        .Select(ps => new ProgramSlotDto(ps.ChannelId, ps.Channel!.Name, ps.Title, ps.Description, ps.StartTimeUtc, ps.EndTimeUtc))
        .Take(50)
        .ToListAsync();

    return TypedResults.Ok(new SearchResultDto(channels, programs));
});

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
