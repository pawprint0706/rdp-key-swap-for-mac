namespace RdpKeySwap.Keyboard;

/// <summary>
/// The only transformation this program performs: a Mac Command press arrives as a Windows
/// key and becomes Alt; a Mac Option press arrives as Alt and becomes a Windows key.
/// Control passes through untouched.
/// </summary>
public static class KeyMap
{
    public static bool TryMap(uint sourceKey, out KeyEmission down)
    {
        down = sourceKey switch
        {
            VirtualKeys.LeftWin => new KeyEmission((ushort)VirtualKeys.LeftMenu, IsExtended: false, IsUp: false),
            VirtualKeys.RightWin => new KeyEmission((ushort)VirtualKeys.RightMenu, IsExtended: true, IsUp: false),
            VirtualKeys.LeftMenu => new KeyEmission((ushort)VirtualKeys.LeftWin, IsExtended: true, IsUp: false),
            VirtualKeys.RightMenu => new KeyEmission((ushort)VirtualKeys.RightWin, IsExtended: true, IsUp: false),
            _ => default,
        };

        return down.VirtualKey != 0;
    }

    public static bool IsMapped(uint sourceKey) => TryMap(sourceKey, out _);
}
