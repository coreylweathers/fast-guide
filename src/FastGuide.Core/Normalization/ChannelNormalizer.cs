using System.Text.RegularExpressions;
using FastGuide.Core.Interfaces;
using FastGuide.Core.Models;
using FuzzySharp;

namespace FastGuide.Core.Normalization;

public sealed partial class ChannelNormalizer : IChannelNormalizer
{
    private static readonly IReadOnlyDictionary<string, int> ProviderPriority = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["Pluto"] = 100,
        ["Plex"] = 90,
        ["Xumo"] = 80
    };

    public string CanonicalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var normalized = name.Trim();
        normalized = MultiSpace().Replace(normalized, " ");
        normalized = Regex.Replace(normalized, "[\\p{P}&&[^&]]", " ");
        normalized = Regex.Replace(normalized, "\\b(24\\/7|channel)\\b", string.Empty, RegexOptions.IgnoreCase);
        normalized = MultiSpace().Replace(normalized, " ");

        return normalized.Trim().ToUpperInvariant();
    }

    public Channel ResolveOrCreate(IReadOnlyCollection<Channel> existingChannels, ProviderChannelPayload incoming, string providerName)
    {
        var incomingCanonical = CanonicalizeName(incoming.ChannelName);

        var exactMatch = existingChannels.FirstOrDefault(c => CanonicalizeName(c.Name) == incomingCanonical);
        if (exactMatch is not null)
        {
            return exactMatch;
        }

        var fuzzyMatch = existingChannels
            .Select(c => new { Channel = c, Score = Fuzz.Ratio(CanonicalizeName(c.Name), incomingCanonical) })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (fuzzyMatch is not null && fuzzyMatch.Score >= 88)
        {
            var selectedProviderPriority = GetHighestPriority(fuzzyMatch.Channel.ProviderChannels.Select(pc => pc.ProviderName));
            var incomingPriority = GetHighestPriority([providerName]);
            if (incomingPriority >= selectedProviderPriority)
            {
                fuzzyMatch.Channel.Name = incoming.ChannelName.Trim();
                fuzzyMatch.Channel.Description ??= incoming.Description;
                fuzzyMatch.Channel.Category ??= incoming.Category;
            }

            return fuzzyMatch.Channel;
        }

        return new Channel
        {
            Name = incoming.ChannelName.Trim(),
            Description = incoming.Description,
            Category = incoming.Category
        };
    }

    private static int GetHighestPriority(IEnumerable<string> providers) => providers
        .Select(p => ProviderPriority.GetValueOrDefault(p, 10))
        .DefaultIfEmpty(10)
        .Max();

    [GeneratedRegex("\\s+")]
    private static partial Regex MultiSpace();
}
