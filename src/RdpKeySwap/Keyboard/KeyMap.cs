namespace RdpKeySwap.Keyboard;

/// <summary>
/// The transformations this program performs: a Mac Command press arrives as a Windows key and
/// becomes Alt; a Mac Option press arrives as Alt and becomes a Windows key. Control passes
/// through untouched.
/// <para>
/// Apple keyboards place Command and Option on the right, while Windows expects Alt (Han/Eng)
/// and Ctrl (Hanja) there. <c>rightOptionAsRightControl</c> sends the right Option to the right
/// Ctrl so the right pair matches that layout.
/// </para>
/// </summary>
public static class KeyMap
{
    public static bool TryMap(uint sourceKey, bool rightOptionAsRightControl, out KeyEmission down)
    {
        down = sourceKey switch
        {
            VirtualKeys.LeftWin => Emit(VirtualKeys.LeftMenu, extended: false),
            VirtualKeys.RightWin => Emit(VirtualKeys.RightMenu, extended: true),
            VirtualKeys.LeftMenu => Emit(VirtualKeys.LeftWin, extended: true),
            VirtualKeys.RightMenu when rightOptionAsRightControl => Emit(VirtualKeys.RightControl, extended: true),
            VirtualKeys.RightMenu => Emit(VirtualKeys.RightWin, extended: true),
            _ => default,
        };

        return down.VirtualKey != 0;
    }

    private static KeyEmission Emit(uint virtualKey, bool extended) =>
        new((ushort)virtualKey, extended, IsUp: false);
}
