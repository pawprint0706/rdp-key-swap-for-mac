using System.Diagnostics;
using Microsoft.Win32;
using RdpKeySwap.Security;

namespace RdpKeySwap.Settings;

/// <summary>
/// Login startup registration.
/// <para>
/// A standard user gets a Run value, which starts the app with the same token. Elevated startup
/// cannot come from the Run key: Windows would need a UAC prompt at every login. It uses a
/// scheduled task with the highest run level instead, which only an elevated process can create.
/// </para>
/// </summary>
public static class AutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "RdpKeySwap";
    private const string TaskName = "RdpKeySwap";
    private const string AutostartArgument = "--autostart";

    public static bool IsEnabled() => HasRunEntry() || HasScheduledTask();

    /// <summary>True when login startup goes through the elevated scheduled task.</summary>
    public static bool UsesScheduledTask() => HasScheduledTask();

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            if (Elevation.IsElevated)
            {
                RegisterElevated();
            }
            else
            {
                WriteRunEntry();
            }

            return;
        }

        RemoveRunEntry();

        if (HasScheduledTask())
        {
            RemoveScheduledTask();
        }
    }

    /// <summary>Registers elevated startup and drops the unelevated Run value so only one copy starts.</summary>
    public static void RegisterElevated()
    {
        CreateScheduledTask();
        RemoveRunEntry();
    }

    private static bool HasRunEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(ValueName) is string value && value.Length > 0;
    }

    private static void WriteRunEntry()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("시작 프로그램 레지스트리 키를 열 수 없습니다.");

        key.SetValue(ValueName, $"\"{Application.ExecutablePath}\" {AutostartArgument}", RegistryValueKind.String);
    }

    private static void RemoveRunEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static bool HasScheduledTask() => RunSchtasks($"/Query /TN \"{TaskName}\"").ExitCode == 0;

    private static void CreateScheduledTask()
    {
        // /RL HIGHEST produces the elevated token (no prompt once registered) and /IT keeps it
        // interactive so it does not ask for the account password.
        var command = $"\"{Application.ExecutablePath}\" {AutostartArgument}".Replace("\"", "\\\"");
        var result = RunSchtasks(
            $"/Create /TN \"{TaskName}\" /TR \"{command}\" /SC ONLOGON /RL HIGHEST /IT /F");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "관리자 권한 로그인 시작 작업을 등록하지 못했습니다.\n\n" + result.Output.Trim());
        }
    }

    private static void RemoveScheduledTask()
    {
        var result = RunSchtasks($"/Delete /TN \"{TaskName}\" /F");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "관리자 권한 로그인 시작 작업을 삭제하지 못했습니다. 관리자 권한이 필요합니다.\n\n" + result.Output.Trim());
        }
    }

    private static (int ExitCode, string Output) RunSchtasks(string arguments)
    {
        var startInfo = new ProcessStartInfo("schtasks.exe", arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("schtasks.exe를 실행하지 못했습니다.");

        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output);
    }
}
