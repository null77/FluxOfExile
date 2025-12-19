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

        // Last Week (hourly) tab
        var hourlyTab = new TabPage("Last 7 Days (Hourly)");
        _hourlyList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _hourlyList.Columns.Add("Date", 100);
        _hourlyList.Columns.Add("Hour", 80);
        _hourlyList.Columns.Add("Minutes", 80);
        _hourlyList.Columns.Add("Daily Total", 100);
        hourlyTab.Controls.Add(_hourlyList);

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

        _tabControl.TabPages.Add(hourlyTab);
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

        // Load hourly data for last 7 days
        _hourlyList.Items.Clear();
        var now = DateTime.Now;
        var weekAgo = now.AddDays(-7);

        var recentRecords = history.HourlyRecords
            .Where(r => r.Hour >= weekAgo)
            .OrderByDescending(r => r.Hour)
            .ToList();

        // Group by date for daily totals
        var dailyTotals = recentRecords
            .GroupBy(r => r.Hour.Date)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.MinutesPlayed));

        foreach (var record in recentRecords)
        {
            if (record.MinutesPlayed < 0.1) continue; // Skip negligible entries

            var item = new ListViewItem(record.Hour.ToString("ddd MMM dd"));
            item.SubItems.Add(record.Hour.ToString("HH:00"));
            item.SubItems.Add($"{record.MinutesPlayed:F1}");

            var dailyTotal = dailyTotals.GetValueOrDefault(record.Hour.Date, 0);
            item.SubItems.Add(FormatTime(dailyTotal));

            _hourlyList.Items.Add(item);
        }

        // Load weekly summary (last 4 weeks from history)
        _weeklyList.Items.Clear();

        // Calculate weeks from all available data
        var allRecords = history.HourlyRecords.OrderBy(r => r.Hour).ToList();
        if (allRecords.Any())
        {
            var firstDate = allRecords.First().Hour.Date;
            var lastDate = now.Date;

            // Get start of each week (Sunday)
            var currentWeekStart = lastDate.AddDays(-(int)lastDate.DayOfWeek);

            for (int i = 0; i < 5; i++) // Show up to 5 weeks
            {
                var weekStart = currentWeekStart.AddDays(-7 * i);
                var weekEnd = weekStart.AddDays(7);

                var weekRecords = history.HourlyRecords
                    .Where(r => r.Hour >= weekStart && r.Hour < weekEnd)
                    .ToList();

                if (!weekRecords.Any()) continue;

                var totalMinutes = weekRecords.Sum(r => r.MinutesPlayed);
                var daysActive = weekRecords.Select(r => r.Hour.Date).Distinct().Count();

                var item = new ListViewItem(weekStart.ToString("MMM dd, yyyy"));
                item.SubItems.Add(FormatTime(totalMinutes));
                item.SubItems.Add(daysActive.ToString());

                _weeklyList.Items.Add(item);
            }
        }

        // Update total
        _totalLabel.Text = $"Total All Time: {FormatTime(history.TotalMinutesAllTime)}";
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
