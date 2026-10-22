using System.ComponentModel;
using System.Runtime.InteropServices;
using RdpKeySwap.Interop;

namespace RdpKeySwap.Keyboard;

/// <summary>
/// Installs WH_KEYBOARD_LL on the calling thread. The thread must pump messages, which the
/// WinForms UI thread already does.
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private readonly Func<RawKeyEvent, HookResult> _handler;
    private readonly NativeMethods.LowLevelKeyboardProc _callback;
    private IntPtr _handle;

    public KeyboardHook(Func<RawKeyEvent, HookResult> handler)
    {
        _handler = handler;
        _callback = Callback;
    }

    public bool IsInstalled => _handle != IntPtr.Zero;

    public void Install()
    {
        if (IsInstalled)
        {
            return;
        }

        _handle = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_KEYBOARD_LL,
            _callback,
            NativeMethods.GetModuleHandleW(null),
            0);

        if (_handle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "저수준 키보드 훅을 설치하지 못했습니다.");
        }
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_handle);
            _handle = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }

    private IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return NativeMethods.CallNextHookEx(_handle, nCode, wParam, lParam);
        }

        var info = Marshal.PtrToStructure<NativeMethods.KbdLlHookStruct>(lParam);
        var message = (int)wParam;
        var raw = new RawKeyEvent(
            info.VirtualKey,
            info.ScanCode,
            message is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP,
            (info.Flags & NativeMethods.LLKHF_EXTENDED) != 0,
            (info.Flags & NativeMethods.LLKHF_INJECTED) != 0,
            info.ExtraInfo);

        HookResult result;
        try
        {
            result = _handler(raw);
        }
        catch
        {
            // Never break the input chain because of a handler failure.
            result = HookResult.PassThrough();
        }

        switch (result.Disposition)
        {
            case HookDisposition.Swallow:
                return (IntPtr)1;
            case HookDisposition.Replace:
                KeyInjector.Send(result.Emission);
                return (IntPtr)1;
            default:
                return NativeMethods.CallNextHookEx(_handle, nCode, wParam, lParam);
        }
    }
}
