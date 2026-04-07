namespace FastGuide.Core.Models;

public sealed record NormalizedProviderData(
    string ProviderName,
    IReadOnlyList<ProviderChannelPayload> Channels,
    IReadOnlyList<ProviderProgramPayload> Programs);

public sealed record ProviderChannelPayload(
    string ProviderChannelId,
    string ChannelName,
    string? Description,
    string? Category,
    string RawMetadata);

public sealed record ProviderProgramPayload(
    string ProviderChannelId,
    string Title,
    string? Description,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string RawMetadata);
