namespace RdpKeySwap.Settings;

public sealed class AppSettings
{
    /// <summary>RDP client names (the connecting Mac machines) that enable the remap.</summary>
    public List<string> ClientNames { get; set; } = new();

    /// <summary>
    /// Send the right Option key to the right Ctrl key instead of the right Windows key.
    /// Apple keyboards place Command and Option on the right, while Windows expects the right
    /// pair to be Alt (Han/Eng) and Ctrl (Hanja).
    /// </summary>
    public bool MapRightOptionToRightControl { get; set; }
}
