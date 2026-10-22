using RdpKeySwap.Security;
using RdpKeySwap.Settings;
using RdpKeySwap.Ui;

namespace RdpKeySwap;

internal static class Program
{
    /// <summary>The "Local\" namespace is per session, so each user session runs its own instance.</summary>
    private const string MutexName = @"Local\RdpKeySwap.SingleInstance";

    /// <summary>Restart-with-elevation hand-off: register elevated startup, then run as administrator.</summary>
    private const string AdminStartupArgument = "--admin-startup";

    /// <summary>How long an elevated hand-off waits for the instance it replaces to exit.</summary>
    private static readonly TimeSpan HandoffTimeout = TimeSpan.FromSeconds(10);

    [STAThread]
    private static int Main(string[] args)
    {
        var options = StartupOptions.Parse(args);

        if (options.Diagnose)
        {
            return KeyDiagnostics.Run();
        }

        using var mutex = new Mutex(initiallyOwned: false, MutexName);
        if (!TryAcquire(mutex, options.AdminStartup ? HandoffTimeout : TimeSpan.Zero))
        {
            if (!options.Autostart)
            {
                MessageBox.Show(
                    "RDP 키 스왑이 이미 실행 중입니다.",
                    "RDP 키 스왑",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return 0;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (options.AdminStartup)
        {
            RegisterElevatedStartup();
        }

        try
        {
            using var context = new TrayApplicationContext(options.Autostart);
            Application.Run(context);
        }
        finally
        {
            mutex.ReleaseMutex();
        }

        return 0;
    }

    /// <summary>
    /// The elevated instance owns the login registration from now on: a scheduled task with the
    /// highest run level, because the Run key cannot start an elevated process without a prompt.
    /// </summary>
    private static void RegisterElevatedStartup()
    {
        try
        {
            if (!Elevation.IsElevated)
            {
                throw new InvalidOperationException("관리자 권한으로 실행되지 않았습니다.");
            }

            AutoStart.RegisterElevated();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "관리자 권한 로그인 시작을 등록하지 못했습니다.\n\n" + ex.Message,
                "RDP 키 스왑",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private static bool TryAcquire(Mutex mutex, TimeSpan timeout)
    {
        try
        {
            return mutex.WaitOne(timeout);
        }
        catch (AbandonedMutexException)
        {
            // The previous instance died without releasing; we now own the mutex.
            return true;
        }
    }

    private sealed record StartupOptions(bool Autostart, bool Diagnose, bool AdminStartup)
    {
        public static StartupOptions Parse(string[] args) => new(
            Autostart: args.Contains("--autostart", StringComparer.OrdinalIgnoreCase),
            Diagnose: args.Contains("--diagnose", StringComparer.OrdinalIgnoreCase),
            AdminStartup: args.Contains(AdminStartupArgument, StringComparer.OrdinalIgnoreCase));
    }
}
