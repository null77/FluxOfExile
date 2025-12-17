namespace FluxOfExile.TechTest;

public partial class Form1 : Form
{
    private OverlayForm _overlay = null!;
    private System.Windows.Forms.Timer _windowScanTimer = null!;
    private IntPtr _attachedWindow = IntPtr.Zero;

    // UI Controls
    private ListBox _windowList = null!;
    private Button _refreshButton = null!;
    private Button _attachButton = null!;
    private Button _detachButton = null!;
    private TrackBar _dimSlider = null!;
    private Label _dimLabel = null!;
    private Label _statusLabel = null!;
    private CheckBox _autoAttachCheckbox = null!;
    private GroupBox _windowGroup = null!;
    private GroupBox _dimGroup = null!;
    private GroupBox _statusGroup = null!;
    private Label _foregroundLabel = null!;
    private Button _presetNone = null!;
    private Button _preset25 = null!;
    private Button _preset50 = null!;
    private Button _preset75 = null!;
    private Button _preset90 = null!;

    public Form1()
    {
        InitializeComponent();
        InitializeCustomControls();

        _overlay = new OverlayForm();

        _windowScanTimer = new System.Windows.Forms.Timer();
        _windowScanTimer.Interval = 1000;
        _windowScanTimer.Tick += WindowScanTimer_Tick;
        _windowScanTimer.Start();

        RefreshWindowList();
    }

    private void InitializeCustomControls()
    {
        Text = "FluxOfExile - Window Dimming Tech Test";
        ClientSize = new Size(500, 550);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        // Window Selection Group
        _windowGroup = new GroupBox
        {
            Text = "Window Selection",
            Location = new Point(10, 10),
            Size = new Size(475, 200)
        };

        _windowList = new ListBox
        {
            Location = new Point(10, 25),
            Size = new Size(455, 120),
            Font = new Font("Consolas", 9)
        };
        _windowList.DoubleClick += WindowList_DoubleClick;

        _refreshButton = new Button
        {
            Text = "Refresh",
            Location = new Point(10, 155),
            Size = new Size(80, 30)
        };
        _refreshButton.Click += RefreshButton_Click;

        _attachButton = new Button
        {
            Text = "Attach",
            Location = new Point(100, 155),
            Size = new Size(80, 30)
        };
        _attachButton.Click += AttachButton_Click;

        _detachButton = new Button
        {
            Text = "Detach",
            Location = new Point(190, 155),
            Size = new Size(80, 30),
            Enabled = false
        };
        _detachButton.Click += DetachButton_Click;

        _autoAttachCheckbox = new CheckBox
        {
            Text = "Auto-attach to PoE windows",
            Location = new Point(290, 160),
            Size = new Size(170, 25),
            Checked = true
        };

        _windowGroup.Controls.AddRange([_windowList, _refreshButton, _attachButton, _detachButton, _autoAttachCheckbox]);

        // Dimming Control Group
        _dimGroup = new GroupBox
        {
            Text = "Dimming Control",
            Location = new Point(10, 220),
            Size = new Size(475, 130)
        };

        _dimSlider = new TrackBar
        {
            Location = new Point(10, 25),
            Size = new Size(380, 45),
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            TickFrequency = 10
        };
        _dimSlider.ValueChanged += DimSlider_ValueChanged;

        _dimLabel = new Label
        {
            Text = "Dim: 0%",
            Location = new Point(395, 30),
            Size = new Size(70, 20),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        // Preset buttons
        _presetNone = new Button { Text = "0%", Location = new Point(10, 80), Size = new Size(60, 30) };
        _preset25 = new Button { Text = "25%", Location = new Point(80, 80), Size = new Size(60, 30) };
        _preset50 = new Button { Text = "50%", Location = new Point(150, 80), Size = new Size(60, 30) };
        _preset75 = new Button { Text = "75%", Location = new Point(220, 80), Size = new Size(60, 30) };
        _preset90 = new Button { Text = "90%", Location = new Point(290, 80), Size = new Size(60, 30) };

        _presetNone.Click += (s, e) => SetDimLevel(0);
        _preset25.Click += (s, e) => SetDimLevel(25);
        _preset50.Click += (s, e) => SetDimLevel(50);
        _preset75.Click += (s, e) => SetDimLevel(75);
        _preset90.Click += (s, e) => SetDimLevel(90);

        _dimGroup.Controls.AddRange([_dimSlider, _dimLabel, _presetNone, _preset25, _preset50, _preset75, _preset90]);

        // Status Group
        _statusGroup = new GroupBox
        {
            Text = "Status",
            Location = new Point(10, 360),
            Size = new Size(475, 140)
        };

        _statusLabel = new Label
        {
            Text = "Not attached to any window",
            Location = new Point(10, 25),
            Size = new Size(455, 40),
            Font = new Font("Segoe UI", 9)
        };

        _foregroundLabel = new Label
        {
            Text = "Foreground: (none)",
            Location = new Point(10, 70),
            Size = new Size(455, 60),
            Font = new Font("Consolas", 8)
        };

        _statusGroup.Controls.AddRange([_statusLabel, _foregroundLabel]);

        Controls.AddRange([_windowGroup, _dimGroup, _statusGroup]);
    }

    private void SetDimLevel(int level)
    {
        _dimSlider.Value = level;
    }

    private void RefreshWindowList()
    {
        _windowList.Items.Clear();

        var windows = WindowEnumerator.GetAllWindows();

        // Sort: PoE windows first, then by title
        var sorted = windows
            .OrderByDescending(w => w.MatchesPoE())
            .ThenBy(w => w.Title)
            .ToList();

        foreach (var window in sorted)
        {
            string prefix = window.MatchesPoE() ? "[PoE] " : "";
            _windowList.Items.Add(new WindowListItem(window, $"{prefix}{window.Title} ({window.ProcessName})"));
        }
    }

    private void WindowScanTimer_Tick(object? sender, EventArgs e)
    {
        // Update foreground window info
        var foreground = WindowEnumerator.GetForegroundWindowInfo();
        if (foreground != null)
        {
            string poeMarker = foreground.MatchesPoE() ? " [PoE DETECTED]" : "";
            _foregroundLabel.Text = $"Foreground: {foreground.Title}\n" +
                                   $"Class: {foreground.ClassName}\n" +
                                   $"Process: {foreground.ProcessName} (PID: {foreground.ProcessId}){poeMarker}";
        }

        // Auto-attach to PoE if enabled and not currently attached
        if (_autoAttachCheckbox.Checked && _attachedWindow == IntPtr.Zero)
        {
            var poeWindows = WindowEnumerator.FindPoEWindows();
            if (poeWindows.Count > 0)
            {
                AttachToWindow(poeWindows[0]);
            }
        }

        // Check if attached window is still valid
        if (_attachedWindow != IntPtr.Zero)
        {
            if (!NativeMethods.IsWindowVisible(_attachedWindow))
            {
                DetachFromWindow();
                _statusLabel.Text = "Target window closed or hidden. Detached.";
            }
        }
    }

    private void RefreshButton_Click(object? sender, EventArgs e)
    {
        RefreshWindowList();
    }

    private void AttachButton_Click(object? sender, EventArgs e)
    {
        if (_windowList.SelectedItem is WindowListItem item)
        {
            AttachToWindow(item.WindowInfo);
        }
    }

    private void WindowList_DoubleClick(object? sender, EventArgs e)
    {
        if (_windowList.SelectedItem is WindowListItem item)
        {
            AttachToWindow(item.WindowInfo);
        }
    }

    private void DetachButton_Click(object? sender, EventArgs e)
    {
        DetachFromWindow();
    }

    private void AttachToWindow(WindowInfo window)
    {
        _attachedWindow = window.Handle;
        _overlay.AttachToWindow(window.Handle);
        _overlay.DimLevel = _dimSlider.Value;

        _statusLabel.Text = $"Attached to: {window.Title}\n" +
                           $"Bounds: {window.Bounds}";

        _attachButton.Enabled = false;
        _detachButton.Enabled = true;
    }

    private void DetachFromWindow()
    {
        _attachedWindow = IntPtr.Zero;
        _overlay.Detach();

        _statusLabel.Text = "Not attached to any window";
        _attachButton.Enabled = true;
        _detachButton.Enabled = false;
    }

    private void DimSlider_ValueChanged(object? sender, EventArgs e)
    {
        _dimLabel.Text = $"Dim: {_dimSlider.Value}%";
        _overlay.DimLevel = _dimSlider.Value;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _windowScanTimer?.Stop();
        _windowScanTimer?.Dispose();
        _overlay?.Dispose();
        base.OnFormClosing(e);
    }

    private class WindowListItem
    {
        public WindowInfo WindowInfo { get; }
        public string DisplayText { get; }

        public WindowListItem(WindowInfo info, string displayText)
        {
            WindowInfo = info;
            DisplayText = displayText;
        }

        public override string ToString() => DisplayText;
    }
}
