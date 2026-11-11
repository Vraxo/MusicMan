using MusicMan.Core;
using System.Text.Json;

namespace MusicMan.Infrastructure;

internal static class SettingsManager
{
    internal static readonly string DataDir = Path.Combine(AppContext.BaseDirectory, "Data");
    private static readonly string SettingsFile = Path.Combine(DataDir, "settings.json");
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new CoverFitModeJsonConverter() }
    };

    public static Settings Current { get; }

    static SettingsManager()
    {
        Settings settings = new();

        try
        {
            if (File.Exists(SettingsFile))
            {
                string content = File.ReadAllText(SettingsFile);
                Settings? loaded = JsonSerializer.Deserialize<Settings>(content, SerializerOptions);
                if (loaded is not null)
                {
                    settings = loaded;
                }
            }
            else
            {
                Directory.CreateDirectory(DataDir);
                string serialized = JsonSerializer.Serialize(settings, SerializerOptions);
                File.WriteAllText(SettingsFile, serialized);
                Log.Info($"Created default configuration file: '{SettingsFile}'");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(
                $"Failed to load or write '{SettingsFile}'. " +
                $"Using default settings. Error: {ex.Message}");
        }

        ResolvePaths(settings, AppContext.BaseDirectory);
        Current = settings;
    }

    internal static void ResolvePaths(Settings settings, string baseDirectory)
    {
        settings.DatabaseDir = ResolvePath(settings.DatabaseDir, baseDirectory);
        settings.CoversDir = ResolvePath(settings.CoversDir, baseDirectory);
        settings.BaseDataDir = ResolvePath(settings.BaseDataDir, baseDirectory);
        settings.CookieFile = ResolvePath(settings.CookieFile, baseDirectory);
        settings.PlaylistCheckReportFile = ResolvePath(settings.PlaylistCheckReportFile, baseDirectory);
        settings.YtDlpDir = ResolvePath(settings.YtDlpDir, baseDirectory);
        settings.FfmpegDir = ResolvePath(settings.FfmpegDir, baseDirectory);
    }

    private static string ResolvePath(string path, string baseDirectory) =>
        string.IsNullOrWhiteSpace(path) ? path : Path.GetFullPath(path, baseDirectory);
}
