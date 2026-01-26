using System.Text.Json;
using FluxOfExile.Models;

namespace FluxOfExile.Services;

public class SettingsService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FluxOfExile");

    private static readonly string SettingsFile = Path.Combine(AppDataFolder, "settings.json");
    private static readonly string StateFile = Path.Combine(AppDataFolder, "state.json");
    private static readonly string HistoryFile = Path.Combine(AppDataFolder, "history.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public Settings Settings { get; private set; } = new();
    public SessionState State { get; private set; } = new();
    public PlayHistory History { get; private set; } = new();

    private int _previousDailyTimeLimit;

    public event Action<int, int>? DailyTimeLimitChanged; // (oldLimit, newLimit)

    public SettingsService()
    {
        EnsureAppDataFolder();
        Load();
        _previousDailyTimeLimit = Settings.DailyTimeLimitMinutes;
    }

    private void EnsureAppDataFolder()
    {
        if (!Directory.Exists(AppDataFolder))
        {
            Directory.CreateDirectory(AppDataFolder);
        }
    }

    public void Load()
    {
        Settings = LoadFile<Settings>(SettingsFile) ?? new Settings();
        State = LoadFile<SessionState>(StateFile) ?? new SessionState();
        History = LoadFile<PlayHistory>(HistoryFile) ?? new PlayHistory();
    }

    public void SaveSettings()
    {
        var oldLimit = _previousDailyTimeLimit;
        var newLimit = Settings.DailyTimeLimitMinutes;

        SaveFile(SettingsFile, Settings);

        if (oldLimit != newLimit)
        {
            _previousDailyTimeLimit = newLimit;
            DailyTimeLimitChanged?.Invoke(oldLimit, newLimit);
        }
    }

    public void SaveState()
    {
        SaveFile(StateFile, State);
    }

    public void SaveHistory()
    {
        SaveFile(HistoryFile, History);
    }

    public void ResetHistory()
    {
        History = new PlayHistory();
        SaveHistory();
    }

    private T? LoadFile<T>(string path) where T : class
    {
        var backupPath = path + ".bak";

        // Try to load main file
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var result = JsonSerializer.Deserialize<T>(json, JsonOptions);
                if (result != null)
                    return result;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading {path}: {ex.Message}");
        }

        // Main file failed or doesn't exist, try backup
        try
        {
            if (File.Exists(backupPath))
            {
                System.Diagnostics.Debug.WriteLine($"Main file corrupted, attempting recovery from {backupPath}");
                var json = File.ReadAllText(backupPath);
                var result = JsonSerializer.Deserialize<T>(json, JsonOptions);
                if (result != null)
                {
                    // Restore from backup
                    File.Copy(backupPath, path, overwrite: true);
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading backup {backupPath}: {ex.Message}");
        }

        return null;
    }

    private void SaveFile<T>(string path, T data)
    {
        var tempPath = path + ".tmp";
        var backupPath = path + ".bak";

        try
        {
            // Serialize data
            var json = JsonSerializer.Serialize(data, JsonOptions);

            // Write to temporary file with forced disk flush
            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write,
                FileShare.None, 4096, FileOptions.WriteThrough))
            using (var writer = new StreamWriter(fileStream))
            {
                writer.Write(json);
                writer.Flush();
                fileStream.Flush(flushToDisk: true);
            }

            // Create backup of existing file before overwriting
            if (File.Exists(path))
            {
                File.Copy(path, backupPath, overwrite: true);
            }

            // Atomic rename: replace main file with temp file
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving {path}: {ex.Message}");

            // Clean up temp file if it exists
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }
}
