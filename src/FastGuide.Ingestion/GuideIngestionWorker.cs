using FastGuide.Infrastructure.Ingestion;

namespace FastGuide.Ingestion;

public sealed class GuideIngestionWorker(IServiceScopeFactory scopeFactory, ILogger<GuideIngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnce(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in ingestion loop.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task RunOnce(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IngestionOrchestrator>();
        await orchestrator.RunIngestionAsync(cancellationToken);
    }
}
