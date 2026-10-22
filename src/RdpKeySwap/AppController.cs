using System.Diagnostics;
using RdpKeySwap.Keyboard;
using RdpKeySwap.Session;
using RdpKeySwap.Settings;

namespace RdpKeySwap;

/// <summary>
/// Owns the hook, the apply condition and the held-key state. Everything runs on the UI
/// thread, which is the thread that installed the hook and therefore receives its callbacks.
/// </summary>
public sealed class AppController : IDisposable
{
    /// <summary>
    /// Session notifications cover connect/disconnect/lock. The poll is the fallback for a
    /// client that reconnects to the same session without a notification that changes the name.
    /// </summary>
    private static readonly TimeSpan RecheckInterval = TimeSpan.FromSeconds(5);

    private readonly SettingsStore _store;
    private readonly SessionWatcher _watcher;
    private readonly KeyboardHook _hook;
    private readonly RemapState _remap = new();
    private readonly System.Windows.Forms.Timer _recheck;
    private readonly int _sessionId;

    private AppSettings _settings;
    private AppStatus _status;
    private bool _disposed;

    public AppController(SettingsStore? store = null)
    {
        _store = store ?? new SettingsStore();
        _settings = _store.Load();
        _sessionId = Process.GetCurrentProcess().SessionId;

        _status = Evaluate();

        _hook = new KeyboardHook(OnKeyEvent);
        _hook.Install();

        _watcher = new SessionWatcher();
        _watcher.Changed += (_, _) => Refresh();
        _watcher.Start();

        _recheck = new System.Windows.Forms.Timer { Interval = (int)RecheckInterval.TotalMilliseconds };
        _recheck.Tick += (_, _) => Refresh();
        _recheck.Start();
    }

    public event EventHandler? StatusChanged;

    public AppSettings Settings => _settings;

    public AppStatus Status => _status;

    public void UpdateClientNames(IEnumerable<string?> names)
    {
        var normalized = ClientNameMatcher.Normalize(names);
        _settings = new AppSettings { ClientNames = normalized.ToList() };
        _store.Save(_settings);
        Refresh();
    }

    /// <summary>Re-reads the session and re-evaluates the apply condition.</summary>
    public void Refresh()
    {
        var previous = _status;
        _status = Evaluate();

        if (previous.IsActive && !_status.IsActive)
        {
            ReleaseHeldKeys();
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _recheck.Stop();
        _recheck.Dispose();

        ReleaseHeldKeys();
        _hook.Dispose();
        _watcher.Dispose();
    }

    private AppStatus Evaluate() => AppStatus.Evaluate(WtsSession.Query(_sessionId), _settings.ClientNames);

    private void ReleaseHeldKeys() => KeyInjector.SendAll(_remap.ReleaseAll());

    private HookDisposition OnKeyEvent(RawKeyEvent e)
    {
        // Our own synthesized events must reach the system untouched.
        if (e.IsInjected && (nuint)e.ExtraInfo == KeyInjector.Marker)
        {
            return HookDisposition.PassThrough;
        }

        var sourceKey = VirtualKeys.Normalize(e.VirtualKey, e.IsExtended);

        // A release that belongs to a key we already released must not reach the system as an
        // unmatched key-up, whether or not the remap is currently active.
        if (e.IsUp && _remap.TryConsumeSwallowedUp(sourceKey))
        {
            return HookDisposition.Swallow;
        }

        if (!_status.IsActive)
        {
            return HookDisposition.PassThrough;
        }

        if (e.IsUp)
        {
            var outcome = _remap.HandleUp(sourceKey, out var up);
            return outcome switch
            {
                // A release the system refuses would leave the synthesized modifier held, so let
                // the original key-up through instead.
                RemapOutcome.Synthesized => KeyInjector.TrySend(up)
                    ? HookDisposition.Swallow
                    : HookDisposition.PassThrough,
                RemapOutcome.Swallowed => HookDisposition.Swallow,
                _ => HookDisposition.PassThrough,
            };
        }

        if (!RemapState.TryGetDown(sourceKey, out var down))
        {
            return HookDisposition.PassThrough;
        }

        // UIPI refuses injected input while a higher-integrity window has focus. Forward the
        // original key then: a key the target never receives must not be swallowed either.
        if (!KeyInjector.TrySend(down))
        {
            return HookDisposition.PassThrough;
        }

        _remap.MarkDown(sourceKey, down);
        return HookDisposition.Swallow;
    }
}
