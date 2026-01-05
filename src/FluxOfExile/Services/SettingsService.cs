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
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading {path}: {ex.Message}");
        }
        return null;
    }

    private void SaveFile<T>(string path, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving {path}: {ex.Message}");
        }
    }
}
