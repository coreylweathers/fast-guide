using FastGuide.Core.Interfaces;
using FastGuide.Core.Models;
using Microsoft.Extensions.Logging;

namespace FastGuide.Infrastructure.Providers;

public sealed class XumoIngestionService(HttpClient httpClient, ILogger<XumoIngestionService> logger)
    : ProviderClientBase(httpClient, logger), IProviderIngestionService
{
    public string ProviderName => "Xumo";

    public async Task<NormalizedProviderData> FetchAsync(CancellationToken cancellationToken)
    {
        var doc = await GetJsonWithRetryAsync("https://stream.xumo.com/channels", cancellationToken);
        if (doc is null || doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return new NormalizedProviderData(ProviderName, [], []);
        }

        var channels = new List<ProviderChannelPayload>();

        foreach (var channel in doc.RootElement.EnumerateArray())
        {
            var id = channel.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N");
            var name = channel.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "Unknown" : "Unknown";

            channels.Add(new ProviderChannelPayload(
                id,
                name,
                channel.TryGetProperty("description", out var descEl) ? descEl.GetString() : null,
                channel.TryGetProperty("genre", out var genreEl) ? genreEl.GetString() : null,
                ToRaw(channel)));
        }

        return new NormalizedProviderData(ProviderName, channels, []);
    }
}
