using RdpKeySwap.Settings;

namespace RdpKeySwap.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "rdpkeyswap-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private SettingsStore CreateStore() => new(Path.Combine(_directory, "settings.json"));

    [Fact]
    public void Saved_names_survive_a_reload()
    {
        var store = CreateStore();
        store.Save(new AppSettings { ClientNames = { "example-mac", "spare-mac" } });

        var loaded = CreateStore().Load();

        Assert.Equal(new[] { "example-mac", "spare-mac" }, loaded.ClientNames);
    }

    [Fact]
    public void Missing_file_yields_defaults()
    {
        var loaded = CreateStore().Load();

        Assert.Empty(loaded.ClientNames);
    }

    [Fact]
    public void Damaged_file_yields_defaults_instead_of_throwing()
    {
        var store = CreateStore();
        Directory.CreateDirectory(_directory);
        File.WriteAllText(store.FilePath, "{ this is not json");

        Assert.Empty(store.Load().ClientNames);
    }

    [Fact]
    public void Save_normalizes_what_it_writes()
    {
        var store = CreateStore();
        store.Save(new AppSettings { ClientNames = { " example-mac ", "EXAMPLE-MAC", "", "other-mac" } });

        var loaded = store.Load();

        Assert.Equal(new[] { "example-mac", "other-mac" }, loaded.ClientNames);
    }

    [Fact]
    public void Right_option_mapping_survives_a_reload()
    {
        var store = CreateStore();
        store.Save(new AppSettings { ClientNames = { "example-mac" }, MapRightOptionToRightControl = true });

        var loaded = CreateStore().Load();

        Assert.True(loaded.MapRightOptionToRightControl);
        Assert.Equal(new[] { "example-mac" }, loaded.ClientNames);
    }

    [Fact]
    public void Right_option_mapping_defaults_to_off()
    {
        Assert.False(CreateStore().Load().MapRightOptionToRightControl);
    }

    [Fact]
    public void Save_replaces_the_previous_file()
    {
        var store = CreateStore();
        store.Save(new AppSettings { ClientNames = { "first-mac" } });
        store.Save(new AppSettings { ClientNames = { "second-mac" } });

        Assert.Equal(new[] { "second-mac" }, store.Load().ClientNames);
        Assert.False(File.Exists(store.FilePath + ".tmp"));
    }
}
