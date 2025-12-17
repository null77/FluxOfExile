using FluxOfExile.Services;

namespace FluxOfExile.Forms;

public class DebugForm : Form
{
    private readonly TimeTracker _timeTracker;
    private readonly SettingsService _settingsService;
    private readonly Action<int> _setDimLevel;

    private Label _statusLabel = null!;
    private TrackBar _timeSlider = null!;
    private Label _timeLabel = null!;
    private TrackBar _dimSlider = null!;
    private Label _dimLabel = null!;
    private CheckBox _overrideDim = null!;
    private Button _resetButton = null!;
    private System.Windows.Forms.Timer _updateTimer = null!;

    public DebugForm(TimeTracker timeTracker, SettingsService settingsService, Action<int> setDimLevel)
    {
        _timeTracker = timeTracker;
        _settingsService = settingsService;
        _setDimLevel = setDimLevel;

        InitializeControls();

        _updateTimer = new System.Windows.Forms.Timer();
        _updateTimer.Interval = 500;
        _updateTimer.Tick += (s, e) => UpdateStatus();
        _updateTimer.Start();
    }

    private void InitializeControls()
    {
        Text = "FluxOfExile Debug Panel";
        ClientSize = new Size(400, 490);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;

        var yPos = 15;

        // Status display
        _statusLabel = new Label
        {
            Location = new Point(15, yPos),
            Size = new Size(370, 80),
            Font = new Font("Consolas", 9),
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(_statusLabel);
        yPos += 95;

        // Time injection
        var timeGroup = new GroupBox
        {
            Text = "Set Accumulated Time",
            Location = new Point(15, yPos),
            Size = new Size(370, 70)
        };

        _timeSlider = new TrackBar
        {
            Location = new Point(10, 20),
            Size = new Size(280, 45),
            Minimum = 0,
            Maximum = 180, // 3 hours max
            Value = 0,
            TickFrequency = 15
        };
        _timeSlider.ValueChanged += TimeSlider_ValueChanged;

        _timeLabel = new Label
        {
            Location = new Point(295, 25),
            Size = new Size(65, 20),
            Text = "0 min"
        };

        timeGroup.Controls.AddRange([_timeSlider, _timeLabel]);
        Controls.Add(timeGroup);
        yPos += 80;

        // Dim override
        var dimGroup = new GroupBox
        {
            Text = "Dim Override",
            Location = new Point(15, yPos),
            Size = new Size(370, 80)
        };

        _overrideDim = new CheckBox
        {
            Text = "Override automatic dimming",
            Location = new Point(10, 20),
            AutoSize = true
        };
        _overrideDim.CheckedChanged += OverrideDim_CheckedChanged;

        _dimSlider = new TrackBar
        {
            Location = new Point(10, 45),
            Size = new Size(280, 45),
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            TickFrequency = 10,
            Enabled = false
        };
        _dimSlider.ValueChanged += DimSlider_ValueChanged;

        _dimLabel = new Label
        {
            Location = new Point(295, 50),
            Size = new Size(65, 20),
            Text = "0%"
        };

        dimGroup.Controls.AddRange([_overrideDim, _dimSlider, _dimLabel]);
        Controls.Add(dimGroup);
        yPos += 90;

        // Reset button and time presets
        _resetButton = new Button
        {
            Text = "Reset Today",
            Location = new Point(15, yPos),
            Size = new Size(85, 30)
        };
        _resetButton.Click += (s, e) =>
        {
            _timeTracker.ResetToday();
            _timeSlider.Value = 0;
        };
        Controls.Add(_resetButton);

        // Preset buttons
        var preset30 = new Button { Text = "30m", Location = new Point(105, yPos), Size = new Size(45, 30) };
        var preset60 = new Button { Text = "60m", Location = new Point(155, yPos), Size = new Size(45, 30) };
        var preset90 = new Button { Text = "90m", Location = new Point(205, yPos), Size = new Size(45, 30) };
        var preset105 = new Button { Text = "105m", Location = new Point(255, yPos), Size = new Size(50, 30) };
        var preset120 = new Button { Text = "120m", Location = new Point(310, yPos), Size = new Size(50, 30) };

        preset30.Click += (s, e) => SetTime(30);
        preset60.Click += (s, e) => SetTime(60);
        preset90.Click += (s, e) => SetTime(90);
        preset105.Click += (s, e) => SetTime(105);
        preset120.Click += (s, e) => SetTime(120);

        Controls.AddRange([preset30, preset60, preset90, preset105, preset120]);
        yPos += 45;

        // Notification test buttons
        var notifyGroup = new GroupBox
        {
            Text = "Test Notifications",
            Location = new Point(15, yPos),
            Size = new Size(370, 70)
        };

        var btnHourly = new Button { Text = "Hourly", Location = new Point(10, 25), Size = new Size(60, 30) };
        var btn30Min = new Button { Text = "30 min", Location = new Point(75, 25), Size = new Size(55, 30) };
        var btn15Min = new Button { Text = "15 min", Location = new Point(135, 25), Size = new Size(55, 30) };
        var btnLimit = new Button { Text = "Limit", Location = new Point(195, 25), Size = new Size(55, 30) };
        var btnOvertime = new Button { Text = "Overtime", Location = new Point(255, 25), Size = new Size(70, 30) };

        btnHourly.Click += (s, e) => _timeTracker.DebugTriggerNotification(NotificationType.Hourly);
        btn30Min.Click += (s, e) => _timeTracker.DebugTriggerNotification(NotificationType.Warning30Min);
        btn15Min.Click += (s, e) => _timeTracker.DebugTriggerNotification(NotificationType.Warning15Min);
        btnLimit.Click += (s, e) => _timeTracker.DebugTriggerNotification(NotificationType.LimitReached);
        btnOvertime.Click += (s, e) => _timeTracker.DebugTriggerNotification(NotificationType.Overtime);

        notifyGroup.Controls.AddRange([btnHourly, btn30Min, btn15Min, btnLimit, btnOvertime]);
        Controls.Add(notifyGroup);
        yPos += 80;

        // Time speed control
        var speedGroup = new GroupBox
        {
            Text = "Time Speed (for simulation)",
            Location = new Point(15, yPos),
            Size = new Size(370, 55)
        };

        var speedSlider = new TrackBar
        {
            Location = new Point(10, 18),
            Size = new Size(280, 45),
            Minimum = 1,
            Maximum = 40,
            Value = 1,
            TickFrequency = 5
        };

        var speedLabel = new Label
        {
            Location = new Point(295, 22),
            Size = new Size(65, 20),
            Text = "1x",
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        speedSlider.ValueChanged += (s, e) =>
        {
            _timeTracker.TimeMultiplier = speedSlider.Value;
            speedLabel.Text = $"{speedSlider.Value}x";
        };

        speedGroup.Controls.AddRange([speedSlider, speedLabel]);
        Controls.Add(speedGroup);
    }

    private void SetTime(int minutes)
    {
        _timeSlider.Value = Math.Min(minutes, _timeSlider.Maximum);
        _timeTracker.DebugSetAccumulatedMinutes(minutes);
    }

    private void UpdateStatus()
    {
        var total = _timeTracker.GetTotalMinutesToday();
        var remaining = _timeTracker.GetMinutesRemaining();
        var dimLevel = _timeTracker.GetCurrentDimLevel();
        var state = _settingsService.State;
        var settings = _settingsService.Settings;

        var status = state.IsPaused ? "PAUSED" :
                    state.CurrentSessionStart != null ? "TRACKING" : "IDLE";

        _statusLabel.Text =
            $"Status: {status}\n" +
            $"Today: {total:F1} min / {settings.DailyTimeLimitMinutes} min limit\n" +
            $"Remaining: {remaining:F1} min\n" +
            $"Auto Dim Level: {dimLevel}%";

        if (!_overrideDim.Checked)
        {
            _dimSlider.Value = dimLevel;
            _dimLabel.Text = $"{dimLevel}%";
        }
    }

    private void TimeSlider_ValueChanged(object? sender, EventArgs e)
    {
        _timeLabel.Text = $"{_timeSlider.Value} min";
        _timeTracker.DebugSetAccumulatedMinutes(_timeSlider.Value);
    }

    private void OverrideDim_CheckedChanged(object? sender, EventArgs e)
    {
        _dimSlider.Enabled = _overrideDim.Checked;

        if (_overrideDim.Checked)
        {
            _setDimLevel(_dimSlider.Value);
        }
    }

    private void DimSlider_ValueChanged(object? sender, EventArgs e)
    {
        _dimLabel.Text = $"{_dimSlider.Value}%";

        if (_overrideDim.Checked)
        {
            _setDimLevel(_dimSlider.Value);
        }
    }

    public bool IsDimOverridden => _overrideDim.Checked;
    public int OverriddenDimLevel => _dimSlider.Value;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        base.OnFormClosing(e);
    }
}
