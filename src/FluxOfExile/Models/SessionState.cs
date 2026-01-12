namespace FluxOfExile.Models;

public class SessionState
{
    /// <summary>
    /// Total minutes accumulated today
    /// </summary>
    public double TodayAccumulatedMinutes { get; set; } = 0;

    /// <summary>
    /// When the current session started (null if not in session)
    /// </summary>
    public DateTime? CurrentSessionStart { get; set; }

    /// <summary>
    /// The date of the last reset
    /// </summary>
    public DateOnly LastResetDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    /// <summary>
    /// Whether tracking is paused by the user
    /// </summary>
    public bool IsPaused { get; set; } = false;

    /// <summary>
    /// Whether the session is auto-paused due to idle (10s no input)
    /// </summary>
    public bool IsIdlePaused { get; set; } = false;

    /// <summary>
    /// Last time an overtime alert was shown
    /// </summary>
    public DateTime? LastOvertimeAlert { get; set; }

    /// <summary>
    /// Accumulated minutes when last hourly notification was shown
    /// </summary>
    public double LastHourlyNotificationAtMinutes { get; set; } = -60;

    /// <summary>
    /// Whether the 30-minute warning has been shown today
    /// </summary>
    public bool Shown30MinWarning { get; set; } = false;

    /// <summary>
    /// Whether the 15-minute warning has been shown today
    /// </summary>
    public bool Shown15MinWarning { get; set; } = false;

    /// <summary>
    /// Whether the limit reached notification has been shown today
    /// </summary>
    public bool ShownLimitReached { get; set; } = false;

    /// <summary>
    /// Whether the launch notification has been shown for the current session
    /// </summary>
    public bool ShownLaunchNotificationThisSession { get; set; } = false;

    /// <summary>
    /// Last time a launch notification was shown (for 10-minute cooldown)
    /// </summary>
    public DateTime? LastLaunchNotificationTime { get; set; }
}
