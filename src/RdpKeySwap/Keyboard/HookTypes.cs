namespace RdpKeySwap.Keyboard;

/// <summary>One keyboard event exactly as the low-level hook reports it.</summary>
public readonly record struct RawKeyEvent(
    uint VirtualKey,
    uint ScanCode,
    bool IsUp,
    bool IsExtended,
    bool IsInjected,
    IntPtr ExtraInfo);

public enum HookDisposition
{
    /// <summary>Let the original event continue to the system.</summary>
    PassThrough,

    /// <summary>Drop the original event; the replacement was already delivered.</summary>
    Swallow,
}
