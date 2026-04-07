namespace FastGuide.Api.Dtos;

public sealed record ChannelDto(Guid Id, string Name, string? Description, string? Category);
public sealed record ProgramSlotDto(Guid ChannelId, string ChannelName, string Title, string? Description, DateTime StartTimeUtc, DateTime EndTimeUtc);
public sealed record SearchResultDto(IReadOnlyList<ChannelDto> Channels, IReadOnlyList<ProgramSlotDto> Programs);
