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
    PassThrough,
    Swallow,
    Replace,
}

public readonly record struct HookResult(HookDisposition Disposition, KeyEmission Emission)
{
    public static HookResult PassThrough() => new(HookDisposition.PassThrough, default);

    public static HookResult Swallow() => new(HookDisposition.Swallow, default);

    public static HookResult Replace(KeyEmission emission) => new(HookDisposition.Replace, emission);
}
