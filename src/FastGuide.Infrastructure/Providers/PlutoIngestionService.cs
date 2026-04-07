using System.Text.Json;
using FastGuide.Core.Interfaces;
using FastGuide.Core.Models;
using Microsoft.Extensions.Logging;

namespace FastGuide.Infrastructure.Providers;

public sealed class PlutoIngestionService(HttpClient httpClient, ILogger<PlutoIngestionService> logger)
    : ProviderClientBase(httpClient, logger), IProviderIngestionService
{
    public string ProviderName => "Pluto";

    public async Task<NormalizedProviderData> FetchAsync(CancellationToken cancellationToken)
    {
        var channelsJson = await GetJsonWithRetryAsync("https://service-channels.clusters.pluto.tv/v1/guide", cancellationToken);
        if (channelsJson is null)
        {
            return new NormalizedProviderData(ProviderName, [], []);
        }

        var channels = new List<ProviderChannelPayload>();
        var programs = new List<ProviderProgramPayload>();

        if (channelsJson.RootElement.TryGetProperty("channels", out var channelArray) && channelArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var channel in channelArray.EnumerateArray())
            {
                var channelId = channel.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N");
                var name = channel.GetProperty("name").GetString() ?? "Unknown";

                channels.Add(new ProviderChannelPayload(
                    channelId,
                    name,
                    channel.TryGetProperty("summary", out var summary) ? summary.GetString() : null,
                    channel.TryGetProperty("category", out var category) ? category.GetString() : null,
                    ToRaw(channel)));

                if (channel.TryGetProperty("timelines", out var timelines) && timelines.ValueKind == JsonValueKind.Array)
                {
                    foreach (var slot in timelines.EnumerateArray())
                    {
                        var start = AsUtcOrDefault(slot, "start", DateTime.UtcNow);
                        var stop = AsUtcOrDefault(slot, "stop", start.AddMinutes(30));
                        var title = slot.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "Unknown" : "Unknown";

                        programs.Add(new ProviderProgramPayload(
                            channelId,
                            title,
                            slot.TryGetProperty("episode", out var descEl) ? descEl.GetString() : null,
                            start,
                            stop,
                            ToRaw(slot)));
                    }
                }
            }
        }

        return new NormalizedProviderData(ProviderName, channels, programs);
    }
}
