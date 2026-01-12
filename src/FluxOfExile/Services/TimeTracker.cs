using FluxOfExile.Models;

namespace FluxOfExile.Services;

public enum NotificationType
{
    Hourly,
    Warning30Min,
    Warning15Min,
    LimitReached,
    Overtime
}

public class TimeTracker
{
    private readonly SettingsService _settingsService;
    private readonly ProcessMonitor _processMonitor;
    private readonly InputMonitor _inputMonitor;

    public event Action? StateChanged;
    public event Action<string, NotificationType>? NotificationTriggered;

    private Settings Settings => _settingsService.Settings;
    private SessionState State => _settingsService.State;

    /// <summary>
    /// Debug time multiplier (1x = normal, 20x = 20 times faster)
    /// </summary>
    public int TimeMultiplier { get; set; } = 1;

    public TimeTracker(SettingsService settingsService, ProcessMonitor processMonitor)
    {
        _settingsService = settingsService;
        _processMonitor = processMonitor;
        _inputMonitor = new InputMonitor();

        // Subscribe to settings changes
        _settingsService.DailyTimeLimitChanged += OnDailyTimeLimitChanged;
    }

    /// <summary>
    /// Call this regularly (e.g., every second) to update tracking
    /// </summary>
    public void Update()
    {
        CheckDayRollover();

        if (State.IsPaused)
        {
            State.IsIdlePaused = false; // Clear idle state when manually paused
            EndSession();
            return;
        }

        var focusedPoE = _processMonitor.GetFocusedPoEWindow();

        if (focusedPoE != null)
        {
            // PoE is focused - check for idle
            var idleSeconds = _inputMonitor.GetIdleSeconds();

            if (idleSeconds >= 10.0)
            {
                // User is idle - auto-pause
                if (!State.IsIdlePaused)
                {
                    State.IsIdlePaused = true;
                    EndSession(); // Save accumulated time
                    _settingsService.SaveState();
                    StateChanged?.Invoke(); // Notify UI
                }
                return; // Don't track time while idle
            }
            else
            {
                // User is active
                if (State.IsIdlePaused)
                {
                    // Resume from idle
                    State.IsIdlePaused = false;
                    _settingsService.SaveState();
                    StateChanged?.Invoke(); // Update tray tooltip
                }

                // Normal tracking flow
                if (State.CurrentSessionStart == null)
                {
                    StartSession();
                }
                else
                {
                    UpdateAccumulatedTime();
                }
            }
        }
        else
        {
            // PoE not focused
            State.IsIdlePaused = false;
            EndSession();
        }

        CheckNotifications();
        StateChanged?.Invoke();
    }

    private void CheckDayRollover()
    {
        var now = DateTime.Now;

        // If current time is before reset time, the reset date is yesterday
        var effectiveDate = now.TimeOfDay < Settings.ResetTime.ToTimeSpan()
            ? DateOnly.FromDateTime(now.AddDays(-1))
            : DateOnly.FromDateTime(now);

        if (State.LastResetDate < effectiveDate)
        {
            // New day - reset everything!
            State.TodayAccumulatedMinutes = 0;
            State.LastResetDate = effectiveDate;
            State.LastOvertimeAlert = null;
            State.LastHourlyNotificationAtMinutes = -60;
            State.Shown30MinWarning = false;
            State.Shown15MinWarning = false;
            State.ShownLimitReached = false;
            _settingsService.SaveState();
        }
    }

    private void StartSession()
    {
        State.CurrentSessionStart = DateTime.Now;
        State.ShownLaunchNotificationThisSession = false;
        _settingsService.SaveState();
    }

    private void EndSession()
    {
        if (State.CurrentSessionStart != null)
        {
            UpdateAccumulatedTime();
            State.CurrentSessionStart = null;
            State.ShownLaunchNotificationThisSession = false;
            _settingsService.SaveState();
        }
    }

    private void UpdateAccumulatedTime()
    {
        if (State.CurrentSessionStart != null)
        {
            var sessionMinutes = (DateTime.Now - State.CurrentSessionStart.Value).TotalMinutes;
            // Apply time multiplier for debug/testing
            var adjustedMinutes = sessionMinutes * TimeMultiplier;
            State.TodayAccumulatedMinutes += adjustedMinutes;
            State.CurrentSessionStart = DateTime.Now; // Reset for next interval
            _settingsService.SaveState();

            // Record to history
            RecordToHistory(adjustedMinutes);
        }
    }

    private void RecordToHistory(double minutes)
    {
        var history = _settingsService.History;
        var currentHour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);

        var record = history.HourlyRecords.FirstOrDefault(r => r.Hour == currentHour);
        if (record == null)
        {
            record = new Models.HourlyPlayRecord { Hour = currentHour, MinutesPlayed = 0 };
            history.HourlyRecords.Add(record);
        }

        record.MinutesPlayed += minutes;
        history.TotalMinutesAllTime += minutes;

        // Prune old records (keep last 8 days for weekly view)
        var cutoff = DateTime.Now.AddDays(-8);
        history.HourlyRecords.RemoveAll(r => r.Hour < cutoff);

        _settingsService.SaveHistory();
    }

    private void CheckNotifications()
    {
        if (!Settings.AlertsEnabled)
            return;

        // Only check notifications when PoE is being tracked
        if (State.CurrentSessionStart == null)
            return;

        var totalMinutes = GetTotalMinutesToday();
        var remaining = Settings.DailyTimeLimitMinutes - totalMinutes;
        var now = DateTime.Now;

        // Launch notification - show on game start after 10 minutes of inactivity
        if (!State.ShownLaunchNotificationThisSession)
        {
            // Only show if it's been 10+ minutes since last launch notification (or never shown)
            var minutesSinceLastNotification = State.LastLaunchNotificationTime.HasValue
                ? (now - State.LastLaunchNotificationTime.Value).TotalMinutes
                : double.MaxValue;

            if (minutesSinceLastNotification >= 10)
            {
                var hours = (int)totalMinutes / 60;
                var mins = (int)totalMinutes % 60;
                var remHours = (int)Math.Max(0, remaining) / 60;
                var remMins = (int)Math.Max(0, remaining) % 60;

                string message;
                if (remaining > 0)
                {
                    if (remHours > 0)
                        message = $"Played {hours}h {mins}m today. {remHours}h {remMins}m remaining.";
                    else
                        message = $"Played {hours}h {mins}m today. {remMins}m remaining.";
                }
                else
                {
                    var overtimeMinutes = (int)Math.Abs(remaining);
                    message = $"Played {hours}h {mins}m today. {overtimeMinutes}m over limit!";
                }

                NotificationTriggered?.Invoke(message, NotificationType.Hourly);
                State.ShownLaunchNotificationThisSession = true;
                State.LastLaunchNotificationTime = now;
                _settingsService.SaveState();
            }
        }

        // 1. Hourly status notification (only when > 30 min remaining)
        // Triggers at start (0 min) and every 60 minutes of accumulated play time
        if (remaining > 30)
        {
            // Calculate the next hourly milestone (0, 60, 120, etc.)
            var nextMilestone = ((int)(State.LastHourlyNotificationAtMinutes / 60) + 1) * 60;
            // Special case: if last notification was at -60 (initial), trigger at 0
            if (State.LastHourlyNotificationAtMinutes < 0)
                nextMilestone = 0;

            if (totalMinutes >= nextMilestone)
            {
                var hours = (int)totalMinutes / 60;
                var mins = (int)totalMinutes % 60;
                var remHours = (int)remaining / 60;
                var remMins = (int)remaining % 60;

                string message;
                if (remHours > 0)
                    message = $"Played {hours}h {mins}m today. {remHours}h {remMins}m remaining.";
                else
                    message = $"Played {hours}h {mins}m today. {remMins}m remaining.";

                NotificationTriggered?.Invoke(message, NotificationType.Hourly);
                State.LastHourlyNotificationAtMinutes = totalMinutes;
                _settingsService.SaveState();
            }
        }

        // 2. 30-minute warning (one-time)
        if (remaining <= 30 && remaining > 15 && !State.Shown30MinWarning)
        {
            NotificationTriggered?.Invoke($"30 minutes remaining in your daily limit!", NotificationType.Warning30Min);
            State.Shown30MinWarning = true;
            _settingsService.SaveState();
        }

        // 3. 15-minute warning (one-time)
        if (remaining <= 15 && remaining > 0 && !State.Shown15MinWarning)
        {
            NotificationTriggered?.Invoke($"15 minutes remaining in your daily limit!", NotificationType.Warning15Min);
            State.Shown15MinWarning = true;
            _settingsService.SaveState();
        }

        // 4. Limit reached (one-time)
        if (remaining <= 0 && !State.ShownLimitReached)
        {
            NotificationTriggered?.Invoke("You've reached your daily time limit!", NotificationType.LimitReached);
            State.ShownLimitReached = true;
            _settingsService.SaveState();
        }

        // 5. Overtime alerts (every 30 minutes past limit)
        if (remaining <= 0)
        {
            var lastAlert = State.LastOvertimeAlert;

            if (lastAlert == null ||
                (now - lastAlert.Value).TotalMinutes >= Models.Settings.OvertimeAlertIntervalMinutes)
            {
                // Don't double-trigger with limit reached
                if (State.ShownLimitReached && (lastAlert == null || (now - lastAlert.Value).TotalMinutes >= Models.Settings.OvertimeAlertIntervalMinutes))
                {
                    var overtimeMinutes = (int)Math.Abs(remaining);
                    if (overtimeMinutes > 0) // Only show if actually over, not exactly at limit
                    {
                        NotificationTriggered?.Invoke($"You are {overtimeMinutes} minutes over your daily limit!", NotificationType.Overtime);
                    }
                    State.LastOvertimeAlert = now;
                    _settingsService.SaveState();
                }
            }
        }
    }

    /// <summary>
    /// Get total minutes played today (including current session)
    /// </summary>
    public double GetTotalMinutesToday()
    {
        var total = State.TodayAccumulatedMinutes;

        if (State.CurrentSessionStart != null)
        {
            total += (DateTime.Now - State.CurrentSessionStart.Value).TotalMinutes;
        }

        return total;
    }

    /// <summary>
    /// Get minutes remaining (negative if over limit)
    /// </summary>
    public double GetMinutesRemaining()
    {
        return Settings.DailyTimeLimitMinutes - GetTotalMinutesToday();
    }

    /// <summary>
    /// Calculate current dim level based on time remaining
    /// </summary>
    public int GetCurrentDimLevel()
    {
        var remaining = GetMinutesRemaining();

        if (remaining >= Models.Settings.WarningThresholdMinutes)
        {
            // Not in warning zone yet
            return 0;
        }
        else if (remaining <= 0)
        {
            // At or past limit - max dim
            return Settings.DimEndPercent;
        }
        else
        {
            // In warning zone - interpolate smoothly
            var progress = 1.0 - (remaining / Models.Settings.WarningThresholdMinutes);
            var dimRange = Settings.DimEndPercent - Models.Settings.DimStartPercent;
            return Models.Settings.DimStartPercent + (int)(progress * dimRange);
        }
    }

    private void OnDailyTimeLimitChanged(int oldLimit, int newLimit)
    {
        // Re-evaluate notification thresholds when limit changes
        var totalMinutes = GetTotalMinutesToday();
        var remaining = newLimit - totalMinutes;

        // If the new limit puts user into overtime/limit reached
        if (remaining <= 0 && !State.ShownLimitReached)
        {
            NotificationTriggered?.Invoke(
                "You've reached your daily time limit!",
                NotificationType.LimitReached);
            State.ShownLimitReached = true;
            _settingsService.SaveState();
        }
        // If new limit puts user into 15-min warning zone
        else if (remaining <= 15 && remaining > 0 && !State.Shown15MinWarning)
        {
            NotificationTriggered?.Invoke(
                "15 minutes remaining in your daily limit!",
                NotificationType.Warning15Min);
            State.Shown15MinWarning = true;
            _settingsService.SaveState();
        }
        // If new limit puts user into 30-min warning zone
        else if (remaining <= 30 && remaining > 15 && !State.Shown30MinWarning)
        {
            NotificationTriggered?.Invoke(
                "30 minutes remaining in your daily limit!",
                NotificationType.Warning30Min);
            State.Shown30MinWarning = true;
            _settingsService.SaveState();
        }

        // Trigger StateChanged to update dim level immediately
        StateChanged?.Invoke();
    }

    public void TogglePause()
    {
        State.IsPaused = !State.IsPaused;
        _settingsService.SaveState();
        StateChanged?.Invoke();
    }

    public void ResetToday()
    {
        State.TodayAccumulatedMinutes = 0;
        State.CurrentSessionStart = null;
        State.LastOvertimeAlert = null;
        State.LastHourlyNotificationAtMinutes = -60;
        State.Shown30MinWarning = false;
        State.Shown15MinWarning = false;
        State.ShownLimitReached = false;
        _settingsService.SaveState();
        StateChanged?.Invoke();
    }

    // Debug method to set accumulated time
    public void DebugSetAccumulatedMinutes(double minutes)
    {
        State.TodayAccumulatedMinutes = minutes;
        State.CurrentSessionStart = null;
        _settingsService.SaveState();
        StateChanged?.Invoke();
    }

    // Debug method to trigger a specific notification type
    public void DebugTriggerNotification(NotificationType type)
    {
        var remaining = GetMinutesRemaining();
        var totalMinutes = GetTotalMinutesToday();

        switch (type)
        {
            case NotificationType.Hourly:
                var hours = (int)totalMinutes / 60;
                var mins = (int)totalMinutes % 60;
                var remHours = (int)Math.Max(0, remaining) / 60;
                var remMins = (int)Math.Max(0, remaining) % 60;
                string message = remHours > 0
                    ? $"Played {hours}h {mins}m today. {remHours}h {remMins}m remaining."
                    : $"Played {hours}h {mins}m today. {remMins}m remaining.";
                NotificationTriggered?.Invoke(message, NotificationType.Hourly);
                break;

            case NotificationType.Warning30Min:
                NotificationTriggered?.Invoke("30 minutes remaining in your daily limit!", NotificationType.Warning30Min);
                break;

            case NotificationType.Warning15Min:
                NotificationTriggered?.Invoke("15 minutes remaining in your daily limit!", NotificationType.Warning15Min);
                break;

            case NotificationType.LimitReached:
                NotificationTriggered?.Invoke("You've reached your daily time limit!", NotificationType.LimitReached);
                break;

            case NotificationType.Overtime:
                var overtimeMinutes = remaining < 0 ? (int)Math.Abs(remaining) : 30;
                NotificationTriggered?.Invoke($"You are {overtimeMinutes} minutes over your daily limit!", NotificationType.Overtime);
                break;
        }
    }
}
