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
        Assert.True(KeyMap.TryMap(source, rightOptionAsRightControl: false, out var down));

        Assert.Equal((ushort)expectedKey, down.VirtualKey);
        Assert.Equal(expectedExtended, down.IsExtended);
        Assert.False(down.IsUp);
    }

    [Fact]
    public void Right_option_can_map_to_right_control_instead()
    {
        Assert.True(KeyMap.TryMap(VirtualKeys.RightMenu, rightOptionAsRightControl: true, out var down));

        Assert.Equal((ushort)VirtualKeys.RightControl, down.VirtualKey);
        Assert.True(down.IsExtended);
    }

    [Fact]
    public void Right_option_mapping_leaves_the_other_keys_alone()
    {
        Assert.True(KeyMap.TryMap(VirtualKeys.LeftMenu, rightOptionAsRightControl: true, out var leftOption));
        Assert.True(KeyMap.TryMap(VirtualKeys.RightWin, rightOptionAsRightControl: true, out var rightCommand));

        Assert.Equal((ushort)VirtualKeys.LeftWin, leftOption.VirtualKey);
        Assert.Equal((ushort)VirtualKeys.RightMenu, rightCommand.VirtualKey);
    }

    [Fact]
    public void Option_mapping_does_not_touch_control_itself()
    {
        Assert.False(KeyMap.TryMap(VirtualKeys.RightControl, rightOptionAsRightControl: true, out var down));
        Assert.Equal(default, down);
    }

    [Theory]
    [InlineData(VirtualKeys.LeftControl)]
    [InlineData(VirtualKeys.RightControl)]
    [InlineData(VirtualKeys.Hangul)]
    [InlineData(VirtualKeys.Hanja)]
    [InlineData(0x41)] // 'A'
    public void Leaves_other_keys_untouched(uint source)
    {
        Assert.False(KeyMap.TryMap(source, rightOptionAsRightControl: false, out var down));
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

        Assert.True(KeyMap.TryMap(right, rightOptionAsRightControl: false, out var rightEmission));
        Assert.True(KeyMap.TryMap(left, rightOptionAsRightControl: false, out var leftEmission));
        Assert.Equal((ushort)VirtualKeys.RightWin, rightEmission.VirtualKey);
        Assert.Equal((ushort)VirtualKeys.LeftWin, leftEmission.VirtualKey);
    }
}
