using System.ComponentModel.DataAnnotations;

namespace FastGuide.Api.Dtos;

public sealed record ChannelDto(
    Guid Id,
    [StringLength(100, MinimumLength = 1)] string Name,
    [StringLength(1000)] string? Description,
    [StringLength(50)] string? Category);

public sealed record ProgramSlotDto(
    Guid ChannelId,
    [StringLength(100, MinimumLength = 1)] string ChannelName,
    [StringLength(200, MinimumLength = 1)] string Title,
    [StringLength(1000)] string? Description,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc);

public sealed record SearchResultDto(
    IReadOnlyList<ChannelDto> Channels,
    IReadOnlyList<ProgramSlotDto> Programs);
