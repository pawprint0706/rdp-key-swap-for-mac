using System.Diagnostics;
using RdpKeySwap.Interop;
using RdpKeySwap.Keyboard;
using RdpKeySwap.Session;
using RdpKeySwap.Settings;

namespace RdpKeySwap;

/// <summary>
/// Prints the raw keyboard events the session delivers and how the remap would treat them.
/// It never converts input; it exists to confirm the actual codes before trusting a rule.
/// </summary>
internal static class KeyDiagnostics
{
    private const uint RunDurationMs = 120_000;

    public static int Run()
    {
        using var output = OpenConsole();
        if (output is null)
        {
            return 1;
        }

        var sessionId = Process.GetCurrentProcess().SessionId;
        var session = WtsSession.Query(sessionId);
        var settings = new SettingsStore().Load();
        var status = AppStatus.Evaluate(session, settings.ClientNames);

        output.WriteLine("RDP 키 스왑 — 입력 진단 (변환하지 않고 관측만 합니다)");
        output.WriteLine($"세션: {session.Kind} (id {session.SessionId}), 클라이언트: {session.ClientName ?? "없음"}");
        output.WriteLine($"현재 설정 기준 적용 여부: {(status.IsActive ? "켜짐" : "꺼짐")} ({status.Reason})");
        output.WriteLine($"우측 Option → 우측 Ctrl: {(settings.MapRightOptionToRightControl ? "켜짐" : "꺼짐")}");
        output.WriteLine("키를 눌러 전달되는 코드를 확인하세요. Esc 로 종료합니다.");
        output.WriteLine();

        using var hook = new KeyboardHook(raw =>
        {
            Print(output, raw, settings.MapRightOptionToRightControl);

            if (raw.VirtualKey == VirtualKeys.Escape && !raw.IsUp)
            {
                NativeMethods.PostQuitMessage(0);
                return HookDisposition.Swallow;
            }

            return HookDisposition.PassThrough;
        });

        hook.Install();
        NativeMethods.SetTimer(IntPtr.Zero, IntPtr.Zero, RunDurationMs, IntPtr.Zero);

        while (NativeMethods.GetMessageW(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            if (message.Message == NativeMethods.WM_TIMER)
            {
                output.WriteLine("진단 시간이 끝났습니다.");
                break;
            }

            NativeMethods.TranslateMessage(ref message);
            NativeMethods.DispatchMessageW(ref message);
        }

        output.WriteLine("진단을 종료했습니다.");
        return 0;
    }

    private static void Print(TextWriter output, RawKeyEvent raw, bool rightOptionAsRightControl)
    {
        var source = VirtualKeys.Normalize(raw.VirtualKey, raw.IsExtended);
        var flags = (raw.IsExtended ? " ext" : string.Empty) + (raw.IsInjected ? " injected" : string.Empty);

        var verdict = KeyMap.TryMap(source, rightOptionAsRightControl, out var mapped)
            ? $"→ 변환 {Describe(mapped.VirtualKey)}{(mapped.IsExtended ? " ext" : string.Empty)}"
            : "→ 그대로";

        output.WriteLine(
            $"{DateTime.Now:HH:mm:ss.fff} {(raw.IsUp ? "up  " : "down")} {Describe(source),-10}"
            + $" vk=0x{raw.VirtualKey:X2} scan=0x{raw.ScanCode:X2}{flags} {verdict}");
    }

    private static string Describe(uint virtualKey) => virtualKey switch
    {
        VirtualKeys.LeftWin => "LWIN",
        VirtualKeys.RightWin => "RWIN",
        VirtualKeys.LeftMenu => "LMENU",
        VirtualKeys.RightMenu => "RMENU",
        VirtualKeys.LeftControl => "LCTRL",
        VirtualKeys.RightControl => "RCTRL",
        VirtualKeys.LeftShift => "LSHIFT",
        VirtualKeys.RightShift => "RSHIFT",
        VirtualKeys.Hangul => "HANGUL",
        VirtualKeys.Hanja => "HANJA",
        VirtualKeys.Escape => "ESC",
        _ => $"VK_0x{virtualKey:X2}",
    };

    private static StreamWriter? OpenConsole()
    {
        if (!NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS) && !NativeMethods.AllocConsole())
        {
            return null;
        }

        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A host console that refuses the code page still shows the ASCII parts of the output.
        }

        return new StreamWriter(Console.OpenStandardOutput(), new System.Text.UTF8Encoding(false))
        {
            AutoFlush = true,
        };
    }
}
