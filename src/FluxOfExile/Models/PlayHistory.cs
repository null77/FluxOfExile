namespace FluxOfExile.Models;

/// <summary>
/// Represents a single hour of playtime
/// </summary>
public class HourlyPlayRecord
{
    /// <summary>
    /// The hour this record represents (truncated to hour)
    /// </summary>
    public DateTime Hour { get; set; }

    /// <summary>
    /// Minutes played during this hour
    /// </summary>
    public double MinutesPlayed { get; set; }
}

/// <summary>
/// Stores all historical playtime data
/// </summary>
public class PlayHistory
{
    /// <summary>
    /// Hourly play records, keyed by hour start time
    /// </summary>
    public List<HourlyPlayRecord> HourlyRecords { get; set; } = new();

    /// <summary>
    /// Total minutes played since tracking began
    /// </summary>
    public double TotalMinutesAllTime { get; set; } = 0;

    /// <summary>
    /// When history tracking was first started (or last reset)
    /// </summary>
    public DateTime TrackingStartedAt { get; set; } = DateTime.Now;
}
