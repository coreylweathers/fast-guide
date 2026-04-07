using FastGuide.Core.Interfaces;
using FastGuide.Core.Normalization;
using FastGuide.Infrastructure.Data;
using FastGuide.Infrastructure.Ingestion;
using FastGuide.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FastGuide.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFastGuideInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FastGuideDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("FastGuide") ?? "Data Source=fastguide.db"));

        services.AddScoped<IChannelNormalizer, ChannelNormalizer>();
        services.AddScoped<IngestionOrchestrator>();

        services.AddHttpClient<PlutoIngestionService>();
        services.AddHttpClient<PlexIngestionService>();
        services.AddHttpClient<XumoIngestionService>();

        services.AddScoped<IProviderIngestionService, PlutoIngestionService>();
        services.AddScoped<IProviderIngestionService, PlexIngestionService>();
        services.AddScoped<IProviderIngestionService, XumoIngestionService>();

        return services;
    }
}
