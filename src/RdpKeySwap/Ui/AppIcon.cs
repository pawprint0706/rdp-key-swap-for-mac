using System.Reflection;

namespace RdpKeySwap.Ui;

/// <summary>
/// The application icon is a single embedded .ico that also becomes the executable's icon, so
/// the file, the window and the tray always show the same artwork at the right size.
/// </summary>
internal static class AppIcon
{
    private const string ResourceName = "RdpKeySwap.Assets.app.ico";

    private static Icon? _window;
    private static Icon? _tray;

    /// <summary>Icon for the settings window and other top-level windows.</summary>
    public static Icon Window => _window ??= Load(SystemInformation.IconSize);

    /// <summary>Icon for the notification area, sized for the current DPI.</summary>
    public static Icon Tray => _tray ??= Load(SystemInformation.SmallIconSize);

    public static void Dispose()
    {
        _window?.Dispose();
        _window = null;
        _tray?.Dispose();
        _tray = null;
    }

    private static Icon Load(Size size)
    {
        var assembly = typeof(AppIcon).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"아이콘 리소스를 찾을 수 없습니다: {ResourceName}");

        using var master = new Icon(stream);
        return new Icon(master, size);
    }
}
