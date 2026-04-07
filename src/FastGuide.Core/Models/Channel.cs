namespace FastGuide.Core.Models;

public class Channel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public ICollection<ProviderChannel> ProviderChannels { get; set; } = new List<ProviderChannel>();
    public ICollection<ProgramSlot> ProgramSlots { get; set; } = new List<ProgramSlot>();
}
