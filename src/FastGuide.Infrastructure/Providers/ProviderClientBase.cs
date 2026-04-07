using System.Net.Http.Json;
using System.Text.Json;
using FastGuide.Core.Models;
using Microsoft.Extensions.Logging;
using Polly;

namespace FastGuide.Infrastructure.Providers;

public abstract class ProviderClientBase(HttpClient httpClient, ILogger logger)
{
    protected async Task<JsonDocument?> GetJsonWithRetryAsync(string url, CancellationToken cancellationToken)
    {
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));

        try
        {
            return await retryPolicy.ExecuteAsync(async () =>
            {
                using var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed provider HTTP fetch from {Url}", url);
            return null;
        }
    }

    protected static string ToRaw(JsonElement element) => JsonSerializer.Serialize(element);

    protected static DateTime AsUtcOrDefault(JsonElement element, string property, DateTime fallback)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String when DateTime.TryParse(value.GetString(), out var parsed) => parsed.ToUniversalTime(),
            _ => fallback
        };
    }
}
