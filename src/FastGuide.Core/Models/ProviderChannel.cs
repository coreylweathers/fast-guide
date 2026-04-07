namespace FastGuide.Core.Models;

public class ProviderChannel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderChannelId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string RawMetadata { get; set; } = "{}";

    public Guid ChannelId { get; set; }
    public Channel? Channel { get; set; }
}
