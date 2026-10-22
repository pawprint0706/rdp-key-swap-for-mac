using System.Text.Json;

namespace RdpKeySwap.Settings;

/// <summary>Loads and saves the per-user settings file under %APPDATA%\RdpKeySwap.</summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsStore(string? filePath = null)
    {
        FilePath = filePath ?? DefaultPath();
    }

    public string FilePath { get; }

    public static string DefaultPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RdpKeySwap",
        "settings.json");

    public AppSettings Load()
    {
        AppSettings? settings;
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // A damaged or unreadable file must not stop the program from running: fall back
            // to the safe default (no registered names, so nothing is remapped).
            return new AppSettings();
        }

        var normalized = ClientNameMatcher.Normalize(settings?.ClientNames);
        return new AppSettings { ClientNames = normalized.ToList() };
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var normalized = new AppSettings
        {
            ClientNames = ClientNameMatcher.Normalize(settings.ClientNames).ToList(),
        };

        var temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(normalized, JsonOptions));
        File.Move(temporaryPath, FilePath, overwrite: true);
    }
}
