using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FluxOfExile.TechTest;

public static class WindowEnumerator
{
    public static List<WindowInfo> GetAllWindows(bool visibleOnly = true)
    {
        var windows = new List<WindowInfo>();

        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            if (visibleOnly && !NativeMethods.IsWindowVisible(hWnd))
                return true;

            var info = GetWindowInfo(hWnd);
            if (info != null && !string.IsNullOrWhiteSpace(info.Title))
            {
                windows.Add(info);
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public static WindowInfo? GetWindowInfo(IntPtr hWnd)
    {
        try
        {
            // Get window title
            int titleLength = NativeMethods.GetWindowTextLength(hWnd);
            var titleBuilder = new StringBuilder(titleLength + 1);
            NativeMethods.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);

            // Get class name
            var classBuilder = new StringBuilder(256);
            NativeMethods.GetClassName(hWnd, classBuilder, classBuilder.Capacity);

            // Get process ID and name
            NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
            string processName = string.Empty;
            try
            {
                var process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
            catch { }

            // Get window bounds (use DWM for more accurate bounds)
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

            return new WindowInfo
            {
                Handle = hWnd,
                Title = titleBuilder.ToString(),
                ClassName = classBuilder.ToString(),
                ProcessId = processId,
                ProcessName = processName,
                Bounds = bounds,
                IsVisible = NativeMethods.IsWindowVisible(hWnd)
            };
        }
        catch
        {
            return null;
        }
    }

    public static List<WindowInfo> FindPoEWindows()
    {
        return GetAllWindows().Where(w => w.MatchesPoE()).ToList();
    }

    public static WindowInfo? GetForegroundWindowInfo()
    {
        var hWnd = NativeMethods.GetForegroundWindow();
        return GetWindowInfo(hWnd);
    }

    public static bool IsPoEFocused()
    {
        var foreground = GetForegroundWindowInfo();
        return foreground?.MatchesPoE() ?? false;
    }
}
