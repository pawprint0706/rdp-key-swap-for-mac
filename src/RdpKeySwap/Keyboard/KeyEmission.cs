namespace RdpKeySwap.Keyboard;

/// <summary>A single key event the program synthesizes instead of the original input.</summary>
public readonly record struct KeyEmission(ushort VirtualKey, bool IsExtended, bool IsUp)
{
    public KeyEmission AsUp() => this with { IsUp = true };
}
