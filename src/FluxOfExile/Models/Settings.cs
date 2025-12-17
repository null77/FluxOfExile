namespace FluxOfExile.Models;

public class Settings
{
    /// <summary>
    /// Daily time limit in minutes (default: 120 = 2 hours)
    /// </summary>
    public int DailyTimeLimitMinutes { get; set; } = 120;

    /// <summary>
    /// Time of day to reset the daily counter (default: 4:00 AM)
    /// </summary>
    public TimeOnly ResetTime { get; set; } = new TimeOnly(4, 0);

    /// <summary>
    /// Dim level at/past time limit (default: 80%)
    /// </summary>
    public int DimEndPercent { get; set; } = 80;

    /// <summary>
    /// Whether to start with Windows
    /// </summary>
    public bool StartWithWindows { get; set; } = false;

    /// <summary>
    /// Whether alerts are enabled
    /// </summary>
    public bool AlertsEnabled { get; set; } = true;

    // Fixed values (not configurable)
    public static int WarningThresholdMinutes => 45;
    public static int DimStartPercent => 0;
    public static int OvertimeAlertIntervalMinutes => 30;
}
