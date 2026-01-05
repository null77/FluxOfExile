using System.Runtime.InteropServices;

namespace FluxOfExile.Services;

/// <summary>
/// Service for monitoring user input activity
/// </summary>
public class InputMonitor
{
    /// <summary>
    /// Gets the number of seconds since the last keyboard or mouse input
    /// </summary>
    /// <returns>Seconds since last input, or 0 if unable to determine</returns>
    public double GetIdleSeconds()
    {
        var lastInput = new NativeMethods.LASTINPUTINFO();
        lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);

        if (NativeMethods.GetLastInputInfo(ref lastInput))
        {
            var idleTime = Environment.TickCount - lastInput.dwTime;
            return idleTime / 1000.0;
        }

        return 0;
    }
}
