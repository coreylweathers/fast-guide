using FastGuide.ServiceDefaults;
using FastGuide.Api.Dtos;
using FastGuide.Infrastructure;
using FastGuide.Infrastructure.Data;
using FastGuide.Infrastructure.Ingestion;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.AddServiceDefaults();
builder.Services.AddFastGuideInfrastructure(builder.Configuration);

var app = builder.Build();

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
