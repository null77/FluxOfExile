using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FluxOfExile.Services;

public class ProcessMonitor
{
    public class PoEWindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public Rectangle Bounds { get; set; }
        public bool IsPoE2 { get; set; }
    }

    /// <summary>
    /// Check if any PoE window is currently the foreground (focused) window
    /// </summary>
    public PoEWindowInfo? GetFocusedPoEWindow()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
            return null;

        var info = GetWindowInfo(foreground);
        if (info != null && IsPoEWindow(info.Title, info.ProcessName))
        {
            return info;
        }

        return null;
    }

    /// <summary>
    /// Find all running PoE windows
    /// </summary>
    public List<PoEWindowInfo> FindAllPoEWindows()
    {
        var windows = new List<PoEWindowInfo>();

        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd))
                return true;

            var info = GetWindowInfo(hWnd);
            if (info != null && IsPoEWindow(info.Title, info.ProcessName))
            {
                windows.Add(info);
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    private PoEWindowInfo? GetWindowInfo(IntPtr hWnd)
    {
        try
        {
            // Get window title
            int titleLength = NativeMethods.GetWindowTextLength(hWnd);
            var titleBuilder = new StringBuilder(titleLength + 1);
            NativeMethods.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
            var title = titleBuilder.ToString();

            if (string.IsNullOrWhiteSpace(title))
                return null;

            // Get process name
            NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
            string processName = string.Empty;
            try
            {
                var process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
            catch { }

            // Get window bounds
            Rectangle bounds;
            if (NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                out NativeMethods.RECT dwmRect, Marshal.SizeOf<NativeMethods.RECT>()) == 0)
            {
                bounds = dwmRect.ToRectangle();
            }
            else
            {
                NativeMethods.GetWindowRect(hWnd, out NativeMethods.RECT rect);
                bounds = rect.ToRectangle();
            }

            return new PoEWindowInfo
            {
                Handle = hWnd,
                Title = title,
                ProcessName = processName,
                Bounds = bounds,
                IsPoE2 = IsPoE2Window(title, processName)
            };
        }
        catch
        {
            return null;
        }
    }

    private bool IsPoEWindow(string title, string processName)
    {
        var titleLower = title.ToLowerInvariant();
        var processLower = processName.ToLowerInvariant();

        return titleLower.Contains("path of exile") ||
               processLower.Contains("pathofexile") ||
               processLower.Contains("pathofexile_x64") ||
               processLower.Contains("pathofexilesteam") ||
               processLower.Contains("pathofexile2");
    }

    private bool IsPoE2Window(string title, string processName)
    {
        var titleLower = title.ToLowerInvariant();
        var processLower = processName.ToLowerInvariant();

        return titleLower.Contains("path of exile 2") ||
               processLower.Contains("pathofexile2");
    }
}
