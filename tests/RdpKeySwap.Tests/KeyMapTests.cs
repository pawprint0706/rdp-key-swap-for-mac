using RdpKeySwap.Keyboard;

namespace RdpKeySwap.Tests;

public class KeyMapTests
{
    [Theory]
    [InlineData(VirtualKeys.LeftWin, VirtualKeys.LeftMenu, false)]
    [InlineData(VirtualKeys.RightWin, VirtualKeys.RightMenu, true)]
    [InlineData(VirtualKeys.LeftMenu, VirtualKeys.LeftWin, true)]
    [InlineData(VirtualKeys.RightMenu, VirtualKeys.RightWin, true)]
    public void Maps_each_modifier_to_its_counterpart(uint source, uint expectedKey, bool expectedExtended)
    {
        Assert.True(KeyMap.TryMap(source, out var down));

        Assert.Equal((ushort)expectedKey, down.VirtualKey);
        Assert.Equal(expectedExtended, down.IsExtended);
        Assert.False(down.IsUp);
    }

    [Theory]
    [InlineData(VirtualKeys.LeftControl)]
    [InlineData(VirtualKeys.RightControl)]
    [InlineData(VirtualKeys.Hangul)]
    [InlineData(VirtualKeys.Hanja)]
    [InlineData(0x41)] // 'A'
    public void Leaves_other_keys_untouched(uint source)
    {
        Assert.False(KeyMap.TryMap(source, out var down));
        Assert.Equal(default, down);
    }

    [Fact]
    public void Normalizes_generic_modifier_codes_by_side()
    {
        Assert.Equal(VirtualKeys.LeftMenu, VirtualKeys.Normalize(VirtualKeys.Menu, extended: false));
        Assert.Equal(VirtualKeys.RightMenu, VirtualKeys.Normalize(VirtualKeys.Menu, extended: true));
        Assert.Equal(VirtualKeys.LeftControl, VirtualKeys.Normalize(VirtualKeys.Control, extended: false));
        Assert.Equal(VirtualKeys.RightControl, VirtualKeys.Normalize(VirtualKeys.Control, extended: true));
    }

    [Fact]
    public void Normalized_generic_alt_then_maps_like_its_side()
    {
        var right = VirtualKeys.Normalize(VirtualKeys.Menu, extended: true);
        var left = VirtualKeys.Normalize(VirtualKeys.Menu, extended: false);

        Assert.True(KeyMap.TryMap(right, out var rightEmission));
        Assert.True(KeyMap.TryMap(left, out var leftEmission));
        Assert.Equal((ushort)VirtualKeys.RightWin, rightEmission.VirtualKey);
        Assert.Equal((ushort)VirtualKeys.LeftWin, leftEmission.VirtualKey);
    }
}
