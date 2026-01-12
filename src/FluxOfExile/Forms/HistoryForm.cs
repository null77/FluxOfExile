using FluxOfExile.Models;
using FluxOfExile.Services;

namespace FluxOfExile.Forms;

public class HistoryForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly TimeTracker _timeTracker;

    private TabControl _tabControl = null!;
    private ListView _hourlyList = null!;
    private ListView _weeklyList = null!;
    private Label _totalLabel = null!;
    private Button _resetButton = null!;
    private Button _editButton = null!;
    private Button _deleteButton = null!;

    public HistoryForm(SettingsService settingsService, TimeTracker timeTracker)
    {
        _settingsService = settingsService;
        _timeTracker = timeTracker;
        InitializeControls();
        LoadData();
    }

    private void InitializeControls()
    {
        Text = "Play Time History";
        ClientSize = new Size(500, 450);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        _tabControl = new TabControl
        {
            Location = new Point(10, 10),
            Size = new Size(480, 350)
        };

        // Last 7 Days (daily) tab
        var dailyTab = new TabPage("Last 7 Days");
        _hourlyList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _hourlyList.Columns.Add("Day", 150);
        _hourlyList.Columns.Add("Time Played", 120);
        dailyTab.Controls.Add(_hourlyList);

        // Weekly summary tab
        var weeklyTab = new TabPage("Weekly Summary");
        _weeklyList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _weeklyList.Columns.Add("Week Starting", 150);
        _weeklyList.Columns.Add("Hours Played", 120);
        _weeklyList.Columns.Add("Days Active", 100);
        weeklyTab.Controls.Add(_weeklyList);

        _tabControl.TabPages.Add(dailyTab);
        _tabControl.TabPages.Add(weeklyTab);
        Controls.Add(_tabControl);

        // Total and reset section
        _totalLabel = new Label
        {
            Location = new Point(10, 370),
            Size = new Size(350, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        Controls.Add(_totalLabel);

        // Edit and Delete buttons
        _deleteButton = new Button
        {
            Text = "Delete Selected",
            Location = new Point(140, 365),
            Size = new Size(100, 30),
            Enabled = false
        };
        _deleteButton.Click += DeleteButton_Click;
        Controls.Add(_deleteButton);

        _editButton = new Button
        {
            Text = "Edit Selected",
            Location = new Point(250, 365),
            Size = new Size(100, 30),
            Enabled = false
        };
        _editButton.Click += EditButton_Click;
        Controls.Add(_editButton);

        _resetButton = new Button
        {
            Text = "Reset All History",
            Location = new Point(360, 365),
            Size = new Size(130, 30)
        };
        _resetButton.Click += ResetButton_Click;
        Controls.Add(_resetButton);

        // Context menu for daily list
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Edit", null, EditMenuItem_Click);
        contextMenu.Items.Add("Delete", null, DeleteMenuItem_Click);
        _hourlyList.ContextMenuStrip = contextMenu;

        // Event handlers
        _hourlyList.DoubleClick += HourlyList_DoubleClick;
        _hourlyList.SelectedIndexChanged += ListView_SelectedIndexChanged;
        _hourlyList.KeyDown += HourlyList_KeyDown;
        _tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

        var trackingSinceLabel = new Label
        {
            Location = new Point(10, 400),
            Size = new Size(350, 20),
            ForeColor = Color.Gray
        };
        trackingSinceLabel.Text = $"Tracking since: {_settingsService.History.TrackingStartedAt:g}";
        Controls.Add(trackingSinceLabel);
    }

    private void LoadData()
    {
        var history = _settingsService.History;
        var resetTime = _settingsService.Settings.ResetTime;
        var now = DateTime.Now;

        // Load daily data for last 7 days (based on reset time)
        _hourlyList.Items.Clear();

        for (int i = 0; i < 7; i++)
        {
            // Calculate the "day" based on reset time
            // A "day" runs from reset time to reset time the next day
            var dayDate = now.Date.AddDays(-i);
            var dayStart = dayDate.Add(resetTime.ToTimeSpan());
            var dayEnd = dayStart.AddDays(1);

            // If we're before today's reset time, shift back a day
            if (i == 0 && now < dayStart)
            {
                dayStart = dayStart.AddDays(-1);
                dayEnd = dayEnd.AddDays(-1);
                dayDate = dayDate.AddDays(-1);
            }

            var dayRecords = history.HourlyRecords
                .Where(r => r.Hour >= dayStart && r.Hour < dayEnd)
                .ToList();

            var totalMinutes = dayRecords.Sum(r => r.MinutesPlayed);

            var item = new ListViewItem(dayDate.ToString("ddd MMM dd"));
            item.SubItems.Add(FormatTime(totalMinutes));
            item.Tag = new { DayStart = dayStart, DayEnd = dayEnd, DayDate = dayDate };

            _hourlyList.Items.Add(item);
        }

        // Load weekly summary (last 5 weeks from history, based on reset time)
        _weeklyList.Items.Clear();

        // Get start of current week (Sunday at reset time)
        var currentWeekStart = now.Date.AddDays(-(int)now.DayOfWeek).Add(resetTime.ToTimeSpan());
        if (now < currentWeekStart)
            currentWeekStart = currentWeekStart.AddDays(-7);

        for (int i = 0; i < 5; i++)
        {
            var weekStart = currentWeekStart.AddDays(-7 * i);
            var weekEnd = weekStart.AddDays(7);

            var weekRecords = history.HourlyRecords
                .Where(r => r.Hour >= weekStart && r.Hour < weekEnd)
                .ToList();

            if (!weekRecords.Any()) continue;

            var totalMinutes = weekRecords.Sum(r => r.MinutesPlayed);
            var daysActive = weekRecords
                .Select(r => GetDayForRecord(r.Hour, resetTime))
                .Distinct()
                .Count();

            var item = new ListViewItem(weekStart.ToString("MMM dd, yyyy"));
            item.SubItems.Add(FormatTime(totalMinutes));
            item.SubItems.Add(daysActive.ToString());

            _weeklyList.Items.Add(item);
        }

        // Update total
        _totalLabel.Text = $"Total All Time: {FormatTime(history.TotalMinutesAllTime)}";
    }

    private static DateTime GetDayForRecord(DateTime recordTime, TimeOnly resetTime)
    {
        // Determine which "day" a record belongs to based on reset time
        var resetDateTime = recordTime.Date.Add(resetTime.ToTimeSpan());
        if (recordTime < resetDateTime)
            return recordTime.Date.AddDays(-1);
        return recordTime.Date;
    }

    private static string FormatTime(double minutes)
    {
        var hours = (int)(minutes / 60);
        var mins = (int)(minutes % 60);

        if (hours > 0)
            return $"{hours}h {mins}m";
        return $"{mins}m";
    }

    private void ResetButton_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to reset all play time history?\n\nThis cannot be undone.",
            "Reset History",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            _settingsService.ResetHistory();
            LoadData();
            MessageBox.Show("History has been reset.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var hasSelection = _hourlyList.SelectedItems.Count > 0;
        var isDailyTab = _tabControl.SelectedIndex == 0;
        _editButton.Enabled = hasSelection && isDailyTab;
        _deleteButton.Enabled = hasSelection && isDailyTab;
    }

    private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        // Update button state when switching tabs
        var hasSelection = _hourlyList.SelectedItems.Count > 0;
        var isDailyTab = _tabControl.SelectedIndex == 0;
        _editButton.Enabled = hasSelection && isDailyTab;
        _deleteButton.Enabled = hasSelection && isDailyTab;
    }

    private void HourlyList_DoubleClick(object? sender, EventArgs e)
    {
        if (_hourlyList.SelectedItems.Count > 0)
            EditSelectedDay();
    }

    private void EditButton_Click(object? sender, EventArgs e)
    {
        EditSelectedDay();
    }

    private void EditMenuItem_Click(object? sender, EventArgs e)
    {
        EditSelectedDay();
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        DeleteSelectedDay();
    }

    private void DeleteMenuItem_Click(object? sender, EventArgs e)
    {
        DeleteSelectedDay();
    }

    private void HourlyList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && _hourlyList.SelectedItems.Count > 0)
        {
            EditSelectedDay();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Delete && _hourlyList.SelectedItems.Count > 0)
        {
            DeleteSelectedDay();
            e.Handled = true;
        }
    }

    private void EditSelectedDay()
    {
        if (_hourlyList.SelectedItems.Count == 0) return;

        // Reload history to ensure we have latest data
        _settingsService.Load();

        // Get day boundaries from stored metadata
        var selectedItem = _hourlyList.SelectedItems[0];
        dynamic metadata = selectedItem.Tag!;
        DateTime dayStart = metadata.DayStart;
        DateTime dayEnd = metadata.DayEnd;
        DateTime dayDate = metadata.DayDate;

        // Get current minutes for this day
        var dayRecords = _settingsService.History.HourlyRecords
            .Where(r => r.Hour >= dayStart && r.Hour < dayEnd)
            .ToList();
        var currentMinutes = dayRecords.Sum(r => r.MinutesPlayed);

        // Check if editing active day
        if (IsActiveDay(dayDate))
        {
            var warning = MessageBox.Show(
                "You're currently tracking playtime. Pause tracking before editing today?\n\nRecommended: Yes",
                "Active Tracking Detected",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (warning == DialogResult.Yes)
            {
                // Pause tracking
                _timeTracker.TogglePause();
            }
            else if (warning == DialogResult.Cancel)
            {
                return; // Cancel edit
            }
            // DialogResult.No continues with edit anyway
        }

        // Open edit dialog
        using var dialog = new EditDayDialog(dayStart, dayEnd, dayDate, currentMinutes);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            if (dialog.DeleteRequested)
            {
                DeleteDayRecords(dayStart, dayEnd);
            }
            else
            {
                UpdateDayRecords(dayStart, dayEnd, dialog.NewMinutes);
            }

            RecalculateTotalAllTime();
            _settingsService.SaveHistory();
            LoadData(); // Refresh display
        }
    }

    private void DeleteSelectedDay()
    {
        if (_hourlyList.SelectedItems.Count == 0) return;

        // Get day boundaries from stored metadata
        var selectedItem = _hourlyList.SelectedItems[0];
        dynamic metadata = selectedItem.Tag!;
        DateTime dayStart = metadata.DayStart;
        DateTime dayEnd = metadata.DayEnd;
        DateTime dayDate = metadata.DayDate;

        var result = MessageBox.Show(
            $"Delete all playtime for {dayDate:ddd MMM dd}?\n\nThis cannot be undone.",
            "Delete Day",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            DeleteDayRecords(dayStart, dayEnd);
            RecalculateTotalAllTime();
            _settingsService.SaveHistory();
            LoadData();
        }
    }

    private void UpdateDayRecords(DateTime dayStart, DateTime dayEnd, double newTotalMinutes)
    {
        // Remove all existing records for this day
        _settingsService.History.HourlyRecords.RemoveAll(r => r.Hour >= dayStart && r.Hour < dayEnd);

        // Add single record at start of day if > 0
        if (newTotalMinutes > 0)
        {
            _settingsService.History.HourlyRecords.Add(new HourlyPlayRecord
            {
                Hour = dayStart,
                MinutesPlayed = newTotalMinutes
            });
        }
    }

    private void DeleteDayRecords(DateTime dayStart, DateTime dayEnd)
    {
        _settingsService.History.HourlyRecords.RemoveAll(r => r.Hour >= dayStart && r.Hour < dayEnd);
    }

    private void RecalculateTotalAllTime()
    {
        _settingsService.History.TotalMinutesAllTime =
            _settingsService.History.HourlyRecords.Sum(r => r.MinutesPlayed);
    }

    private bool IsActiveDay(DateTime dayDate)
    {
        var now = DateTime.Now;
        var resetTime = _settingsService.Settings.ResetTime;

        // Calculate effective date based on reset time
        var effectiveDate = now.TimeOfDay < resetTime.ToTimeSpan()
            ? DateOnly.FromDateTime(now.AddDays(-1))
            : DateOnly.FromDateTime(now);

        var recordDay = DateOnly.FromDateTime(dayDate);
        return recordDay >= effectiveDate;
    }
}
