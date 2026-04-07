using FastGuide.Core.Models;

namespace FastGuide.Core.Interfaces;

public interface IProviderIngestionService
{
    string ProviderName { get; }
    Task<NormalizedProviderData> FetchAsync(CancellationToken cancellationToken);
}
