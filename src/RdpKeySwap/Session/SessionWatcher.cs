using RdpKeySwap.Interop;

namespace RdpKeySwap.Session;

/// <summary>Raises <see cref="Changed"/> when the session connects, disconnects, locks, or reconnects.</summary>
public sealed class SessionWatcher : NativeWindow, IDisposable
{
    private bool _registered;

    public SessionWatcher()
    {
        CreateHandle(new CreateParams());
    }

    public event EventHandler? Changed;

    public void Start()
    {
        if (_registered || Handle == IntPtr.Zero)
        {
            return;
        }

        _registered = NativeMethods.WTSRegisterSessionNotification(Handle, NativeMethods.NOTIFY_FOR_THIS_SESSION);
    }

    public void Dispose()
    {
        if (_registered)
        {
            NativeMethods.WTSUnRegisterSessionNotification(Handle);
            _registered = false;
        }

        if (Handle != IntPtr.Zero)
        {
            DestroyHandle();
        }

        GC.SuppressFinalize(this);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_WTSSESSION_CHANGE)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref m);
    }
}
