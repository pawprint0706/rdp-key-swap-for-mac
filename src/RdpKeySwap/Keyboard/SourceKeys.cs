namespace RdpKeySwap.Keyboard;

/// <summary>
/// Turns a raw hook event into the virtual key the rules work with.
/// <para>
/// The Korean 101 layouts translate the right-Alt scancode into VK_HANGUL before the hook sees
/// the event, so a physical right Option never arrives as VK_RMENU there. Its scancode still
/// identifies it, and the rules only use that when the right Option mapping is enabled: with
/// the mapping off the event keeps its Han/Eng meaning and must not be touched.
/// </para>
/// </summary>
public static class SourceKeys
{
    /// <summary>Scan code of the Alt key; the extended bit makes it the right-hand one.</summary>
    public const uint AltScanCode = 0x38;

    public static uint Resolve(uint virtualKey, uint scanCode, bool extended, bool rightOptionAsRightControl)
    {
        if (rightOptionAsRightControl
            && virtualKey == VirtualKeys.Hangul
            && scanCode == AltScanCode
            && extended)
        {
            return VirtualKeys.RightMenu;
        }

        return VirtualKeys.Normalize(virtualKey, extended);
    }
}
