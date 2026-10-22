using RdpKeySwap.Ui;

namespace RdpKeySwap;

internal static class Program
{
    /// <summary>The "Local\" namespace is per session, so each user session runs its own instance.</summary>
    private const string MutexName = @"Local\RdpKeySwap.SingleInstance";

    [STAThread]
    private static int Main(string[] args)
    {
        var options = StartupOptions.Parse(args);

        if (options.Diagnose)
        {
            return KeyDiagnostics.Run();
        }

        using var mutex = new Mutex(initiallyOwned: false, MutexName);
        if (!TryAcquire(mutex))
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

    private static bool TryAcquire(Mutex mutex)
    {
        try
        {
            return mutex.WaitOne(TimeSpan.Zero);
        }
        catch (AbandonedMutexException)
        {
            // The previous instance died without releasing; we now own the mutex.
            return true;
        }
    }

    private sealed record StartupOptions(bool Autostart, bool Diagnose)
    {
        public static StartupOptions Parse(string[] args) => new(
            Autostart: args.Contains("--autostart", StringComparer.OrdinalIgnoreCase),
            Diagnose: args.Contains("--diagnose", StringComparer.OrdinalIgnoreCase));
    }
}
