using FastGuide.Core.Models;

namespace FastGuide.Core.Interfaces;

public interface IChannelNormalizer
{
    string CanonicalizeName(string name);
    Channel ResolveOrCreate(IReadOnlyCollection<Channel> existingChannels, ProviderChannelPayload incoming, string providerName);
}
