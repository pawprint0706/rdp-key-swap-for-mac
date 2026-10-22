using RdpKeySwap.Session;
using RdpKeySwap.Settings;

namespace RdpKeySwap;

public enum InactiveReason
{
    None,
    SessionUnknown,
    LocalSession,
    ClientNameUnavailable,
    NoRegisteredNames,
    ClientNotRegistered,
}

/// <summary>Snapshot of the conditions that decide whether input is currently remapped.</summary>
public sealed record AppStatus(
    int SessionId,
    SessionKind Kind,
    string? ClientName,
    bool IsActive,
    InactiveReason Reason)
{
    public static AppStatus Evaluate(SessionInfo session, IEnumerable<string?> registeredNames)
    {
        var reason = DetermineReason(session, registeredNames);

        return new AppStatus(
            session.SessionId,
            session.Kind,
            session.ClientName,
            reason == InactiveReason.None,
            reason);
    }

    private static InactiveReason DetermineReason(SessionInfo session, IEnumerable<string?> registeredNames)
    {
        if (session.Kind == SessionKind.Unknown)
        {
            return InactiveReason.SessionUnknown;
        }

        if (session.Kind != SessionKind.Remote)
        {
            return InactiveReason.LocalSession;
        }

        if (session.ClientName is null)
        {
            return InactiveReason.ClientNameUnavailable;
        }

        var names = ClientNameMatcher.Normalize(registeredNames);
        if (names.Count == 0)
        {
            return InactiveReason.NoRegisteredNames;
        }

        return ClientNameMatcher.Matches(session.ClientName, names)
            ? InactiveReason.None
            : InactiveReason.ClientNotRegistered;
    }
}
