namespace RdpKeySwap.Settings;

public sealed class AppSettings
{
    /// <summary>RDP client names (the connecting Mac machines) that enable the remap.</summary>
    public List<string> ClientNames { get; set; } = new();
}
