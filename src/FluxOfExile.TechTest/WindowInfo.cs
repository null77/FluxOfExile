namespace FluxOfExile.TechTest;

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public uint ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public Rectangle Bounds { get; set; }
    public bool IsVisible { get; set; }

    public override string ToString()
    {
        return $"{Title} [{ClassName}] (PID: {ProcessId} - {ProcessName})";
    }

    public bool MatchesPoE()
    {
        // Check for Path of Exile 1 or 2 by title or process name
        var titleLower = Title.ToLowerInvariant();
        var processLower = ProcessName.ToLowerInvariant();

        return titleLower.Contains("path of exile") ||
               processLower.Contains("pathofexile") ||
               processLower.Contains("pathofexile_x64") ||
               processLower.Contains("pathofexilesteam") ||
               processLower.Contains("pathofexile2");
    }
}
