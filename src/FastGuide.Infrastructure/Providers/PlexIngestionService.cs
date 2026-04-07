using System.Text.Json;
using FastGuide.Core.Interfaces;
using FastGuide.Core.Models;
using Microsoft.Extensions.Logging;

namespace FastGuide.Infrastructure.Providers;

public sealed class PlexIngestionService(HttpClient httpClient, ILogger<PlexIngestionService> logger)
    : ProviderClientBase(httpClient, logger), IProviderIngestionService
{
    public string ProviderName => "Plex";

    public async Task<NormalizedProviderData> FetchAsync(CancellationToken cancellationToken)
    {
        var doc = await GetJsonWithRetryAsync("https://epg.provider.plex.tv/grid", cancellationToken);
        if (doc is null)
        {
            return new NormalizedProviderData(ProviderName, [], []);
        }

        var channels = new List<ProviderChannelPayload>();
        var programs = new List<ProviderProgramPayload>();

        if (doc.RootElement.TryGetProperty("MediaContainer", out var media)
            && media.TryGetProperty("Channels", out var channelArray)
            && channelArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var channel in channelArray.EnumerateArray())
            {
                var id = channel.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N");
                var name = channel.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "Unknown" : "Unknown";

                channels.Add(new ProviderChannelPayload(
                    id,
                    name,
                    channel.TryGetProperty("summary", out var summary) ? summary.GetString() : null,
                    channel.TryGetProperty("genre", out var genre) ? genre.GetString() : null,
                    ToRaw(channel)));

                if (channel.TryGetProperty("programs", out var programArray) && programArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var program in programArray.EnumerateArray())
                    {
                        var start = AsUtcOrDefault(program, "start", DateTime.UtcNow);
                        var stop = AsUtcOrDefault(program, "stop", start.AddMinutes(30));

                        programs.Add(new ProviderProgramPayload(
                            id,
                            program.TryGetProperty("title", out var pt) ? pt.GetString() ?? "Unknown" : "Unknown",
                            program.TryGetProperty("summary", out var pd) ? pd.GetString() : null,
                            start,
                            stop,
                            ToRaw(program)));
                    }
                }
            }
        }

        return new NormalizedProviderData(ProviderName, channels, programs);
    }
}
