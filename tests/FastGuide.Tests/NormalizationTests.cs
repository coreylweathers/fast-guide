using FastGuide.Core.Models;
using FastGuide.Core.Normalization;
using Xunit;

namespace FastGuide.Tests;

public class NormalizationTests
{
    private readonly ChannelNormalizer _normalizer = new();

    [Fact]
    public void CanonicalizeName_RemovesNoiseTokens()
    {
        var canonical = _normalizer.CanonicalizeName("  Action Channel 24/7! ");
        Assert.Equal("ACTION", canonical);
    }

    [Fact]
    public void ResolveOrCreate_UsesFuzzyMatch_WhenClose()
    {
        var existing = new List<Channel>
        {
            new() { Name = "Movie Hub", ProviderChannels = [new ProviderChannel { ProviderName = "Xumo" }] }
        };

        var resolved = _normalizer.ResolveOrCreate(existing, new ProviderChannelPayload("1", "MovieHub", null, null, "{}"), "Pluto");

        Assert.Same(existing[0], resolved);
    }
}
