using FastGuide.ServiceDefaults;
using FastGuide.Infrastructure;
using FastGuide.Infrastructure.Data;
using FastGuide.Ingestion;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddFastGuideInfrastructure(builder.Configuration);
builder.Services.AddHostedService<GuideIngestionWorker>();

builder.Services.AddSerilog((services, cfg) => cfg
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console());

using var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FastGuideDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
