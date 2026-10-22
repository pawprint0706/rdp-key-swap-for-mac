using RdpKeySwap;
using RdpKeySwap.Session;

namespace RdpKeySwap.Tests;

public class AppStatusTests
{
    private static SessionInfo Remote(string? clientName = "example-mac") =>
        new(1, SessionKind.Remote, clientName);

    [Fact]
    public void Active_only_when_remote_and_name_matches()
    {
        var status = AppStatus.Evaluate(Remote("EXAMPLE-MAC"), new[] { " example-mac " });

        Assert.True(status.IsActive);
        Assert.Equal(InactiveReason.None, status.Reason);
        Assert.Equal("EXAMPLE-MAC", status.ClientName);
    }

    [Fact]
    public void Local_session_is_inactive()
    {
        var status = AppStatus.Evaluate(new SessionInfo(2, SessionKind.Console, null), new[] { "example-mac" });

        Assert.False(status.IsActive);
        Assert.Equal(InactiveReason.LocalSession, status.Reason);
    }

    [Fact]
    public void Unknown_session_type_is_inactive()
    {
        var status = AppStatus.Evaluate(new SessionInfo(3, SessionKind.Unknown, "example-mac"), new[] { "example-mac" });

        Assert.False(status.IsActive);
        Assert.Equal(InactiveReason.SessionUnknown, status.Reason);
    }

    [Fact]
    public void Missing_client_name_is_inactive()
    {
        var status = AppStatus.Evaluate(Remote(null), new[] { "example-mac" });

        Assert.False(status.IsActive);
        Assert.Equal(InactiveReason.ClientNameUnavailable, status.Reason);
    }

    [Fact]
    public void No_registered_names_is_inactive()
    {
        var status = AppStatus.Evaluate(Remote(), Array.Empty<string>());

        Assert.False(status.IsActive);
        Assert.Equal(InactiveReason.NoRegisteredNames, status.Reason);
    }

    [Fact]
    public void Different_client_name_is_inactive()
    {
        var status = AppStatus.Evaluate(Remote("office-pc"), new[] { "example-mac", "other-mac" });

        Assert.False(status.IsActive);
        Assert.Equal(InactiveReason.ClientNotRegistered, status.Reason);
    }

    [Fact]
    public void Any_registered_name_can_activate_the_remap()
    {
        var status = AppStatus.Evaluate(Remote("second-mac"), new[] { "first-mac", "second-mac" });

        Assert.True(status.IsActive);
    }
}
