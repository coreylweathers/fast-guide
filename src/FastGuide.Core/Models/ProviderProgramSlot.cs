namespace FastGuide.Core.Models;

public class ProviderProgramSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderChannelId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public string RawMetadata { get; set; } = "{}";

    public Guid? ProgramSlotId { get; set; }
    public ProgramSlot? ProgramSlot { get; set; }
}
