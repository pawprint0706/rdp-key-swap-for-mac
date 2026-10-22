namespace RdpKeySwap.Keyboard;

/// <summary>
/// Virtual-key codes used by the remap rules, plus normalization of the ambiguous
/// generic modifier codes into their left/right variants.
/// </summary>
public static class VirtualKeys
{
    public const uint Shift = 0x10;
    public const uint Control = 0x11;
    public const uint Menu = 0x12;

    public const uint Hangul = 0x15;
    public const uint Hanja = 0x19;
    public const uint Escape = 0x1B;

    public const uint LeftWin = 0x5B;
    public const uint RightWin = 0x5C;

    public const uint LeftShift = 0xA0;
    public const uint RightShift = 0xA1;
    public const uint LeftControl = 0xA2;
    public const uint RightControl = 0xA3;
    public const uint LeftMenu = 0xA4;
    public const uint RightMenu = 0xA5;

    /// <summary>
    /// Turns the generic modifier codes Windows may report (VK_MENU, VK_CONTROL, VK_SHIFT)
    /// into the side-specific codes. The extended flag distinguishes the right-hand keys.
    /// </summary>
    public static uint Normalize(uint virtualKey, bool extended) => virtualKey switch
    {
        Menu => extended ? RightMenu : LeftMenu,
        Control => extended ? RightControl : LeftControl,
        Shift => extended ? RightShift : LeftShift,
        _ => virtualKey,
    };
}
