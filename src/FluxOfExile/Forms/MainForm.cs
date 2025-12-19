using FluxOfExile.Services;

namespace FluxOfExile.Forms;

public class MainForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly ProcessMonitor _processMonitor;
    private readonly TimeTracker _timeTracker;
    private readonly OverlayForm _overlay;

    private NotifyIcon _trayIcon = null!;
    private ContextMenuStrip _trayMenu = null!;
    private System.Windows.Forms.Timer _updateTimer = null!;
    private DebugForm? _debugForm;
    private ToolStripMenuItem _pauseMenuItem = null!;
    private ToolStripMenuItem _statusMenuItem = null!;

    private IntPtr _currentPoEWindow = IntPtr.Zero;

    public MainForm()
    {
        _settingsService = new SettingsService();
        _processMonitor = new ProcessMonitor();
        _timeTracker = new TimeTracker(_settingsService, _processMonitor);
        _overlay = new OverlayForm();

        InitializeComponent();
        InitializeTrayIcon();
        InitializeTimer();

        _timeTracker.StateChanged += OnStateChanged;
        _timeTracker.NotificationTriggered += OnNotificationTriggered;

        // Hide main form - we're a tray app
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Visible = false;
    }

    private void InitializeComponent()
    {
        Text = "FluxOfExile";
        ClientSize = new Size(1, 1);
        FormBorderStyle = FormBorderStyle.None;
        Opacity = 0;
    }

    private void InitializeTrayIcon()
    {
        _trayMenu = new ContextMenuStrip();

        _statusMenuItem = new ToolStripMenuItem("Starting...")
        {
            Enabled = false
        };

        _pauseMenuItem = new ToolStripMenuItem("Pause Tracking", null, (s, e) => TogglePause());

        var settingsItem = new ToolStripMenuItem("Settings...", null, (s, e) => ShowSettings());
        var historyItem = new ToolStripMenuItem("Play History...", null, (s, e) => ShowHistory());
        var debugItem = new ToolStripMenuItem("Debug Panel...", null, (s, e) => ShowDebugPanel());
        var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitApplication());

        _trayMenu.Items.Add(_statusMenuItem);
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(_pauseMenuItem);
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(settingsItem);
        _trayMenu.Items.Add(historyItem);
        _trayMenu.Items.Add(debugItem);
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Text = "FluxOfExile",
            ContextMenuStrip = _trayMenu,
            Visible = true
        };

        _trayIcon.DoubleClick += (s, e) => ShowDebugPanel();
    }

    private Icon CreateDefaultIcon()
    {
        // Try to load custom icon from file
        var iconPath = Path.Combine(AppContext.BaseDirectory, "icon.png");
        if (File.Exists(iconPath))
        {
            try
            {
                using var image = Image.FromFile(iconPath);
                var bitmap = new Bitmap(image, 32, 32);
                return Icon.FromHandle(bitmap.GetHicon());
            }
            catch { }
        }

        // Fallback to simple icon
        var fallback = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(fallback))
        {
            g.Clear(Color.DarkOrange);
            g.FillEllipse(Brushes.Orange, 2, 2, 12, 12);
        }
        return Icon.FromHandle(fallback.GetHicon());
    }

    private void InitializeTimer()
    {
        _updateTimer = new System.Windows.Forms.Timer();
        _updateTimer.Interval = 1000; // Update every second
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        // Update time tracking
        _timeTracker.Update();

        // Check for PoE window
        var poeWindow = _processMonitor.GetFocusedPoEWindow();

        if (poeWindow != null)
        {
            // PoE is focused
            if (_currentPoEWindow != poeWindow.Handle)
            {
                _currentPoEWindow = poeWindow.Handle;
                _overlay.AttachToWindow(_currentPoEWindow);
            }

            // Update dim level (unless debug override is active)
            if (_debugForm == null || !_debugForm.IsDimOverridden)
            {
                _overlay.DimLevel = _timeTracker.GetCurrentDimLevel();
            }
        }
        else
        {
            // PoE not focused - overlay will auto-hide via its own logic
            _currentPoEWindow = IntPtr.Zero;
        }

        UpdateTrayStatus();
    }

    private void UpdateTrayStatus()
    {
        var total = _timeTracker.GetTotalMinutesToday();
        var limit = _settingsService.Settings.DailyTimeLimitMinutes;
        var remaining = _timeTracker.GetMinutesRemaining();

        var hours = (int)total / 60;
        var mins = (int)total % 60;

        string status;
        if (_settingsService.State.IsPaused)
        {
            status = $"Paused - {hours}h {mins}m / {limit / 60}h {limit % 60}m";
        }
        else if (remaining < 0)
        {
            status = $"OVERTIME - {hours}h {mins}m ({Math.Abs(remaining):F0}m over)";
        }
        else
        {
            status = $"{hours}h {mins}m / {limit / 60}h {limit % 60}m ({remaining:F0}m left)";
        }

        _statusMenuItem.Text = status;
        _trayIcon.Text = $"FluxOfExile\n{status}";
    }

    private void OnStateChanged()
    {
        UpdateTrayStatus();
    }

    private void OnNotificationTriggered(string message, NotificationType type)
    {
        var icon = type switch
        {
            NotificationType.Hourly => ToolTipIcon.Info,
            NotificationType.Warning30Min => ToolTipIcon.Info,
            NotificationType.Warning15Min => ToolTipIcon.Warning,
            NotificationType.LimitReached => ToolTipIcon.Warning,
            NotificationType.Overtime => ToolTipIcon.Warning,
            _ => ToolTipIcon.Info
        };

        _trayIcon.ShowBalloonTip(5000, "FluxOfExile", message, icon);
    }

    private void TogglePause()
    {
        _timeTracker.TogglePause();
        _pauseMenuItem.Text = _settingsService.State.IsPaused ? "Resume Tracking" : "Pause Tracking";
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settingsService);
        form.ShowDialog();
    }

    private void ShowHistory()
    {
        using var form = new HistoryForm(_settingsService);
        form.ShowDialog();
    }

    private void ShowDebugPanel()
    {
        if (_debugForm == null || _debugForm.IsDisposed)
        {
            _debugForm = new DebugForm(_timeTracker, _settingsService, SetDimLevel);
            _debugForm.Show();
        }
        else
        {
            _debugForm.BringToFront();
        }
    }

    private void SetDimLevel(int level)
    {
        _overlay.DimLevel = level;
    }

    private void ExitApplication()
    {
        _updateTimer?.Stop();
        _trayIcon.Visible = false;
        _overlay?.Dispose();
        Application.Exit();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            Hide();
        }
        else
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            _trayIcon?.Dispose();
            _overlay?.Dispose();
            _debugForm?.Dispose();
        }
        base.OnFormClosing(e);
    }

    protected override void SetVisibleCore(bool value)
    {
        // Prevent the form from ever showing
        if (!IsHandleCreated)
        {
            CreateHandle();
            value = false;
        }
        base.SetVisibleCore(value);
    }
}
