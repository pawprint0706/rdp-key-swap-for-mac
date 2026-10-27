using RdpKeySwap.Keyboard;

namespace RdpKeySwap.Tests;

public class SourceKeysTests
{
    [Fact]
    public void Right_alt_scancode_arriving_as_hangul_is_the_right_option_when_enabled()
    {
        var source = SourceKeys.Resolve(VirtualKeys.Hangul, SourceKeys.AltScanCode, extended: true, rightOptionAsRightControl: true);

        Assert.Equal(VirtualKeys.RightMenu, source);
    }

    [Fact]
    public void Same_event_keeps_its_han_eng_meaning_when_the_mapping_is_off()
    {
        var source = SourceKeys.Resolve(VirtualKeys.Hangul, SourceKeys.AltScanCode, extended: true, rightOptionAsRightControl: false);

        Assert.Equal(VirtualKeys.Hangul, source);
        Assert.False(KeyMap.TryMap(source, rightOptionAsRightControl: false, out _));
    }

    [Fact]
    public void Dedicated_han_eng_key_is_never_mistaken_for_the_right_option()
    {
        // Korean keyboards send Han/Eng with its own scancode, not the Alt one.
        var source = SourceKeys.Resolve(VirtualKeys.Hangul, 0x72, extended: false, rightOptionAsRightControl: true);

        Assert.Equal(VirtualKeys.Hangul, source);
    }

    [Fact]
    public void Left_alt_scancode_is_not_the_right_option()
    {
        var source = SourceKeys.Resolve(VirtualKeys.Hangul, SourceKeys.AltScanCode, extended: false, rightOptionAsRightControl: true);

        Assert.Equal(VirtualKeys.Hangul, source);
    }

    [Fact]
    public void Other_modifiers_are_resolved_as_before()
    {
        Assert.Equal(VirtualKeys.RightWin, SourceKeys.Resolve(VirtualKeys.RightWin, 0x5C, true, true));
        Assert.Equal(VirtualKeys.RightMenu, SourceKeys.Resolve(VirtualKeys.Menu, SourceKeys.AltScanCode, true, true));
        Assert.Equal(VirtualKeys.LeftMenu, SourceKeys.Resolve(VirtualKeys.Menu, SourceKeys.AltScanCode, false, true));
        Assert.Equal(VirtualKeys.RightControl, SourceKeys.Resolve(VirtualKeys.RightControl, 0x1D, true, true));
    }
}
