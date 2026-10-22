using Microsoft.Win32;

namespace RdpKeySwap.Settings;

/// <summary>
/// Registers the app for the current user's login through the Run key. Only this app's own
/// value is ever written or removed.
/// </summary>
public static class AutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "RdpKeySwap";
    private const string AutostartArgument = "--autostart";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(ValueName) is string value && value.Length > 0;
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("시작 프로그램 레지스트리 키를 열 수 없습니다.");

        if (enabled)
        {
            key.SetValue(ValueName, BuildCommandLine(), RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    private static string BuildCommandLine() => $"\"{Application.ExecutablePath}\" {AutostartArgument}";
}
