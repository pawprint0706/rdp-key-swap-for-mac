using System.Runtime.InteropServices;
using RdpKeySwap.Interop;

namespace RdpKeySwap.Keyboard;

/// <summary>Sends the synthesized key events and marks them so the hook can recognize its own input.</summary>
public static class KeyInjector
{
    /// <summary>Tag written into dwExtraInfo of every synthesized event ("RKSW").</summary>
    public const nuint Marker = 0x524B5357;

    public static void Send(KeyEmission emission)
    {
        var input = new NativeMethods.Input
        {
            Type = NativeMethods.INPUT_KEYBOARD,
            Data = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.KeyboardInput
                {
                    // No virtual key: the event must be translated by the active keyboard layout,
                    // exactly as the physical key would be. Passing VK_RMENU here would bypass the
                    // Korean 101 layout, which maps scan 0x38+extended to VK_HANGUL, and the app
                    // would receive plain Alt instead of the Han/Eng key.
                    VirtualKey = 0,
                    ScanCode = (ushort)ScanCodeFor(emission.VirtualKey),
                    Flags = NativeMethods.KEYEVENTF_SCANCODE
                        | (emission.IsUp ? NativeMethods.KEYEVENTF_KEYUP : 0u)
                        | (emission.IsExtended ? NativeMethods.KEYEVENTF_EXTENDEDKEY : 0u),
                    ExtraInfo = (IntPtr)(long)Marker,
                },
            },
        };

        NativeMethods.SendInput(1, [input], Marshal.SizeOf<NativeMethods.Input>());
    }

    public static void SendAll(IEnumerable<KeyEmission> emissions)
    {
        foreach (var emission in emissions)
        {
            Send(emission);
        }
    }

    /// <summary>
    /// Explicit scan codes keep the Korean 101-key interpretation intact: the right Alt that
    /// the IME turns into a Han/Eng toggle must arrive as scan code 0x38 with the extended flag.
    /// </summary>
    private static uint ScanCodeFor(ushort virtualKey) => virtualKey switch
    {
        (ushort)VirtualKeys.LeftMenu or (ushort)VirtualKeys.RightMenu => 0x38,
        (ushort)VirtualKeys.LeftWin => 0x5B,
        (ushort)VirtualKeys.RightWin => 0x5C,
        (ushort)VirtualKeys.LeftControl or (ushort)VirtualKeys.RightControl => 0x1D,
        _ => NativeMethods.MapVirtualKeyW(virtualKey, NativeMethods.MAPVK_VK_TO_VSC),
    };
}
