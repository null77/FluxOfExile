using FluxOfExile.Models;
using FluxOfExile.Services;

namespace FluxOfExile.Forms;

public class HistoryForm : Form
{
    private readonly SettingsService _settingsService;

    private TabControl _tabControl = null!;
    private ListView _hourlyList = null!;
    private ListView _weeklyList = null!;
    private Label _totalLabel = null!;
    private Button _resetButton = null!;

    public HistoryForm(SettingsService settingsService)
    {
        _settingsService = settingsService;
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

        _resetButton = new Button
        {
            Text = "Reset All History",
            Location = new Point(370, 365),
            Size = new Size(120, 30)
        };
        _resetButton.Click += ResetButton_Click;
        Controls.Add(_resetButton);

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
}
