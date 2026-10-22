using System.Runtime.InteropServices;
using RdpKeySwap.Interop;

namespace RdpKeySwap.Session;

public enum SessionKind
{
    Unknown,
    Console,
    Remote,
}

public sealed record SessionInfo(int SessionId, SessionKind Kind, string? ClientName);

/// <summary>Reads the session type and RDP client name from the Windows Terminal Services API.</summary>
public static class WtsSession
{
    private const short ProtocolConsole = 0;
    private const short ProtocolRemoteDesktop = 2;

    public static SessionInfo Query(int sessionId) =>
        new(sessionId, QueryKind(sessionId), QueryClientName(sessionId));

    private static SessionKind QueryKind(int sessionId)
    {
        if (!NativeMethods.WTSQuerySessionInformationW(
                IntPtr.Zero, sessionId, NativeMethods.WtsInfoClass.ClientProtocolType, out var buffer, out var bytes))
        {
            return SessionKind.Unknown;
        }

        try
        {
            if (bytes < 2)
            {
                return SessionKind.Unknown;
            }

            return Marshal.ReadInt16(buffer) switch
            {
                ProtocolConsole => SessionKind.Console,
                ProtocolRemoteDesktop => SessionKind.Remote,
                _ => SessionKind.Unknown,
            };
        }
        finally
        {
            NativeMethods.WTSFreeMemory(buffer);
        }
    }

    private static string? QueryClientName(int sessionId)
    {
        if (!NativeMethods.WTSQuerySessionInformationW(
                IntPtr.Zero, sessionId, NativeMethods.WtsInfoClass.ClientName, out var buffer, out _))
        {
            return null;
        }

        try
        {
            var name = Marshal.PtrToStringUni(buffer);
            return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        }
        finally
        {
            NativeMethods.WTSFreeMemory(buffer);
        }
    }
}
