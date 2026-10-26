using RdpKeySwap.Keyboard;

namespace RdpKeySwap.Tests;

public class RemapStateTests
{
    private static KeyEmission Press(RemapState state, uint sourceKey)
    {
        Assert.True(state.TryGetDown(sourceKey, out var down));
        state.MarkDown(sourceKey, down);
        return down;
    }

    [Fact]
    public void Release_mirrors_the_press_it_replaced()
    {
        var state = new RemapState();
        var down = Press(state, VirtualKeys.RightWin);

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

        var first = Press(state, VirtualKeys.LeftWin);
        var second = Press(state, VirtualKeys.LeftWin);

        Assert.Equal(first, second);
        Assert.Equal(1, state.HeldCount);
    }

    [Fact]
    public void Unmapped_key_has_no_replacement()
    {
        var state = new RemapState();

        Assert.False(state.TryGetDown(VirtualKeys.LeftControl, out var down));
        Assert.Equal(default, down);
        Assert.False(state.TryGetDown(0x41, out _));
    }

    [Fact]
    public void Release_of_a_key_whose_press_was_forwarded_passes_through()
    {
        // The press was never marked (injection refused, or the program started mid-press), so
        // the system saw the original key go down and needs the matching key-up.
        var state = new RemapState();

        Assert.Equal(RemapOutcome.NotMapped, state.HandleUp(VirtualKeys.RightWin, out var up));
        Assert.Equal(default, up);
        Assert.Equal(0, state.HeldCount);
    }

    [Fact]
    public void Releasing_everything_produces_releases_and_drops_the_later_key_up()
    {
        var state = new RemapState();
        Press(state, VirtualKeys.LeftWin);
        Press(state, VirtualKeys.RightMenu);

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
        Press(state, VirtualKeys.LeftWin);
        state.ReleaseAll();

        Press(state, VirtualKeys.LeftWin);

        Assert.False(state.TryConsumeSwallowedUp(VirtualKeys.LeftWin));
        Assert.Equal(RemapOutcome.Synthesized, state.HandleUp(VirtualKeys.LeftWin, out var up));
        Assert.True(up.IsUp);
    }
}
