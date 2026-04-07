using FastGuide.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FastGuide.Tests;

public class IngestionParsingTests
{
    [Fact]
    public async Task XumoProvider_HandlesInvalidJsonGracefully()
    {
        var client = new HttpClient(new StubHttpHandler("not-json"));
        var service = new XumoIngestionService(client, NullLogger<XumoIngestionService>.Instance);

        var payload = await service.FetchAsync(CancellationToken.None);

        Assert.Empty(payload.Channels);
        Assert.Empty(payload.Programs);
    }
}

public class StubHttpHandler(string payload) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(payload)
        });
}
