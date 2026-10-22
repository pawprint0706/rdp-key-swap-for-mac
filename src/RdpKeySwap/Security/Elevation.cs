using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace RdpKeySwap.Security;

/// <summary>
/// Elevation state and the UAC hand-off used to restart the app with an administrator token.
/// </summary>
public static class Elevation
{
    public static bool IsElevated { get; } = CheckElevated();

    /// <summary>
    /// Starts another instance with a UAC prompt and returns false when the user declines.
    /// The caller keeps running in that case.
    /// </summary>
    public static bool TryStartElevated(string arguments)
    {
        var startInfo = new ProcessStartInfo(Application.ExecutablePath, arguments)
        {
            UseShellExecute = true,
            Verb = "runas",
        };

        try
        {
            Process.Start(startInfo);
            return true;
        }
        catch (Win32Exception)
        {
            // 1223: the elevation prompt was cancelled.
            return false;
        }
    }

    private static bool CheckElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
