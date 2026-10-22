using RdpKeySwap.Keyboard;

namespace RdpKeySwap.Tests;

public class RemapStateTests
{
    [Fact]
    public void Release_mirrors_the_press_it_replaced()
    {
        var state = new RemapState();

        Assert.Equal(RemapOutcome.Synthesized, state.HandleDown(VirtualKeys.RightWin, out var down));
        Assert.Equal(RemapOutcome.Synthesized, state.HandleUp(VirtualKeys.RightWin, out var up));

        Assert.Equal(down.VirtualKey, up.VirtualKey);
        Assert.Equal(down.IsExtended, up.IsExtended);
        Assert.False(down.IsUp);
        Assert.True(up.IsUp);
        Assert.Equal(0, state.HeldCount);
    }

    [Fact]
    public void Auto_repeat_keeps_emitting_the_same_replacement()
    {
        var state = new RemapState();

        state.HandleDown(VirtualKeys.LeftWin, out var first);
        state.HandleDown(VirtualKeys.LeftWin, out var second);

        Assert.Equal(first, second);
        Assert.Equal(1, state.HeldCount);
    }

    [Fact]
    public void Unmapped_key_is_forwarded_without_state()
    {
        var state = new RemapState();

        Assert.Equal(RemapOutcome.NotMapped, state.HandleDown(VirtualKeys.LeftControl, out var down));
        Assert.Equal(default, down);
        Assert.Equal(0, state.HeldCount);

        Assert.Equal(RemapOutcome.NotMapped, state.HandleUp(VirtualKeys.LeftControl, out var up));
        Assert.Equal(default, up);
    }

    [Fact]
    public void Releasing_everything_produces_releases_and_drops_the_later_key_up()
    {
        var state = new RemapState();
        state.HandleDown(VirtualKeys.LeftWin, out _);
        state.HandleDown(VirtualKeys.RightMenu, out _);

        var releases = state.ReleaseAll();

        Assert.Equal(2, releases.Count);
        Assert.All(releases, release => Assert.True(release.IsUp));
        Assert.Contains(releases, release => release.VirtualKey == (ushort)VirtualKeys.LeftMenu);
        Assert.Contains(releases, release => release.VirtualKey == (ushort)VirtualKeys.RightWin);
        Assert.Equal(0, state.HeldCount);

        Assert.True(state.TryConsumeSwallowedUp(VirtualKeys.LeftWin));
        Assert.False(state.TryConsumeSwallowedUp(VirtualKeys.LeftWin));
        Assert.Equal(RemapOutcome.Swallowed, state.HandleUp(VirtualKeys.RightMenu, out var swallowed));
        Assert.Equal(default, swallowed);
    }

    [Fact]
    public void New_press_after_release_clears_the_pending_swallow()
    {
        var state = new RemapState();
        state.HandleDown(VirtualKeys.LeftWin, out _);
        state.ReleaseAll();

        Assert.Equal(RemapOutcome.Synthesized, state.HandleDown(VirtualKeys.LeftWin, out _));
        Assert.False(state.TryConsumeSwallowedUp(VirtualKeys.LeftWin));
        Assert.Equal(RemapOutcome.Synthesized, state.HandleUp(VirtualKeys.LeftWin, out var up));
        Assert.True(up.IsUp);
    }

    [Fact]
    public void Orphan_release_of_a_mapped_key_is_dropped()
    {
        var state = new RemapState();

        Assert.Equal(RemapOutcome.Swallowed, state.HandleUp(VirtualKeys.RightWin, out var emission));
        Assert.Equal(default, emission);
    }
}
