using RdpKeySwap.Settings;

namespace RdpKeySwap.Tests;

public class ClientNameMatcherTests
{
    [Theory]
    [InlineData("example-mac", "example-mac")]
    [InlineData("example-mac", "EXAMPLE-MAC")]
    [InlineData("  example-mac  ", "example-mac")]
    [InlineData("example-mac", "  example-mac")]
    public void Matches_regardless_of_case_and_surrounding_space(string client, string registered)
    {
        Assert.True(ClientNameMatcher.Matches(client, new[] { registered }));
    }

    [Fact]
    public void Matches_any_registered_name()
    {
        var names = new[] { "first-mac", "second-mac", "third-mac" };

        Assert.True(ClientNameMatcher.Matches("second-mac", names));
    }

    [Theory]
    [InlineData("example-mac", "example")]
    [InlineData("example-mac", "example-mac-2")]
    [InlineData("example-mac", "")]
    public void Rejects_partial_or_empty_entries(string client, string registered)
    {
        Assert.False(ClientNameMatcher.Matches(client, new[] { registered }));
    }

    [Fact]
    public void Rejects_missing_client_name()
    {
        Assert.False(ClientNameMatcher.Matches(null, new[] { "example-mac" }));
        Assert.False(ClientNameMatcher.Matches("   ", new[] { "example-mac" }));
    }

    [Fact]
    public void Rejects_empty_list()
    {
        Assert.False(ClientNameMatcher.Matches("example-mac", Array.Empty<string>()));
        Assert.False(ClientNameMatcher.Matches("example-mac", null));
    }

    [Fact]
    public void Normalize_trims_drops_blanks_and_collapses_duplicates()
    {
        var normalized = ClientNameMatcher.Normalize(new[] { " example-mac ", "", "  ", "EXAMPLE-MAC", "other-mac" });

        Assert.Equal(new[] { "example-mac", "other-mac" }, normalized);
    }
}
