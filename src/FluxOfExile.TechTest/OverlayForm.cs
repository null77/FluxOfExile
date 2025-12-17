using System.ComponentModel;

namespace FluxOfExile.TechTest;

public class OverlayForm : Form
{
    private IntPtr _targetWindow;
    private System.Windows.Forms.Timer _updateTimer;
    private int _dimLevel = 0; // 0-100

    public OverlayForm()
    {
        // Configure form for overlay behavior
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Black;
        Opacity = 0;
        StartPosition = FormStartPosition.Manual;

        // Setup update timer
        _updateTimer = new System.Windows.Forms.Timer();
        _updateTimer.Interval = 50; // 20 FPS update rate
        _updateTimer.Tick += UpdateTimer_Tick;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        MakeClickThrough();
    }

    private void MakeClickThrough()
    {
        // Get current extended style
        int exStyle = NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_EXSTYLE);

        // Add layered, transparent (click-through), tool window, and no-activate styles
        exStyle |= NativeMethods.WS_EX_LAYERED;
        exStyle |= NativeMethods.WS_EX_TRANSPARENT;
        exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
        exStyle |= NativeMethods.WS_EX_NOACTIVATE;

        NativeMethods.SetWindowLong(Handle, NativeMethods.GWL_EXSTYLE, exStyle);
    }

    public void AttachToWindow(IntPtr targetWindow)
    {
        _targetWindow = targetWindow;

        if (_targetWindow != IntPtr.Zero)
        {
            UpdatePosition();
            Show();
            _updateTimer.Start();
        }
        else
        {
            _updateTimer.Stop();
            Hide();
        }
    }

    public void Detach()
    {
        _targetWindow = IntPtr.Zero;
        _updateTimer.Stop();
        Hide();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int DimLevel
    {
        get => _dimLevel;
        set
        {
            _dimLevel = Math.Clamp(value, 0, 100);
            UpdateOpacity();
        }
    }

    private void UpdateOpacity()
    {
        // Convert 0-100 dim level to opacity
        // 0% dim = fully transparent (opacity 0)
        // 100% dim = nearly opaque (opacity ~0.9, never fully black)
        Opacity = _dimLevel / 100.0 * 0.9;
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (_targetWindow == IntPtr.Zero)
            return;

        // Check if target window still exists and is visible
        if (!NativeMethods.IsWindowVisible(_targetWindow))
        {
            Hide();
            return;
        }

        // Only show overlay when target window is the foreground window
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground != _targetWindow)
        {
            if (Visible) Hide();
            return;
        }

        UpdatePosition();

        if (!Visible && _dimLevel > 0)
        {
            Show();
        }
    }

    private void UpdatePosition()
    {
        if (_targetWindow == IntPtr.Zero)
            return;

        // Get target window bounds using DWM for accuracy
        Rectangle bounds;
        if (NativeMethods.DwmGetWindowAttribute(_targetWindow, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
            out NativeMethods.RECT dwmRect, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.RECT>()) == 0)
        {
            bounds = dwmRect.ToRectangle();
        }
        else
        {
            NativeMethods.GetWindowRect(_targetWindow, out NativeMethods.RECT rect);
            bounds = rect.ToRectangle();
        }

        // Position overlay to match target window
        // Use SetWindowPos to avoid flickering and maintain topmost
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HWND_TOPMOST,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // Make the form click-through from creation
            cp.ExStyle |= NativeMethods.WS_EX_LAYERED;
            cp.ExStyle |= NativeMethods.WS_EX_TRANSPARENT;
            cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            cp.ExStyle |= NativeMethods.WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
