using FluxOfExile.Models;
using FluxOfExile.Services;

namespace FluxOfExile.Forms;

public class SettingsForm : Form
{
    private readonly SettingsService _settingsService;

    private NumericUpDown _timeLimitHours = null!;
    private NumericUpDown _timeLimitMinutes = null!;
    private DateTimePicker _resetTime = null!;
    private NumericUpDown _dimEnd = null!;
    private CheckBox _alertsEnabled = null!;
    private CheckBox _startWithWindows = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    public SettingsForm(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeControls();
        LoadSettings();
    }

    private void InitializeControls()
    {
        Text = "FluxOfExile Settings";
        ClientSize = new Size(400, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var yPos = 15;
        var controlX = 165;

        // Daily Time Limit
        AddLabel("Daily Time Limit:", 15, yPos);
        _timeLimitHours = new NumericUpDown
        {
            Location = new Point(controlX, yPos - 3),
            Size = new Size(60, 25),
            Minimum = 0,
            Maximum = 24,
            Value = 2
        };
        var hoursLabel = new Label { Text = "hrs", Location = new Point(controlX + 65, yPos), AutoSize = true };
        _timeLimitMinutes = new NumericUpDown
        {
            Location = new Point(controlX + 95, yPos - 3),
            Size = new Size(60, 25),
            Minimum = 0,
            Maximum = 59,
            Value = 0
        };
        var minsLabel = new Label { Text = "mins", Location = new Point(controlX + 160, yPos), AutoSize = true };
        Controls.AddRange([_timeLimitHours, hoursLabel, _timeLimitMinutes, minsLabel]);
        yPos += 35;

        // Reset Time
        AddLabel("Daily Reset Time:", 15, yPos);
        _resetTime = new DateTimePicker
        {
            Location = new Point(controlX, yPos - 3),
            Size = new Size(100, 25),
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true
        };
        Controls.Add(_resetTime);
        yPos += 35;

        // Dim End (max dimming level)
        AddLabel("Max Dim Level:", 15, yPos);
        _dimEnd = new NumericUpDown
        {
            Location = new Point(controlX, yPos - 3),
            Size = new Size(60, 25),
            Minimum = 0,
            Maximum = 100,
            Value = 80
        };
        var dimEndPct = new Label { Text = "% (at/past limit)", Location = new Point(controlX + 65, yPos), AutoSize = true };
        Controls.AddRange([_dimEnd, dimEndPct]);
        yPos += 40;

        // Alerts Enabled
        _alertsEnabled = new CheckBox
        {
            Text = "Enable alerts/notifications",
            Location = new Point(15, yPos),
            AutoSize = true,
            Checked = true
        };
        Controls.Add(_alertsEnabled);
        yPos += 30;

        // Start with Windows
        _startWithWindows = new CheckBox
        {
            Text = "Start with Windows",
            Location = new Point(15, yPos),
            AutoSize = true
        };
        Controls.Add(_startWithWindows);
        yPos += 45;

        // Buttons
        _saveButton = new Button
        {
            Text = "Save",
            Location = new Point(ClientSize.Width - 180, yPos),
            Size = new Size(75, 30),
            DialogResult = DialogResult.OK
        };
        _saveButton.Click += SaveButton_Click;

        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(ClientSize.Width - 95, yPos),
            Size = new Size(75, 30),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange([_saveButton, _cancelButton]);

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;
    }

    private void AddLabel(string text, int x, int y)
    {
        var label = new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true
        };
        Controls.Add(label);
    }

    private void LoadSettings()
    {
        var s = _settingsService.Settings;

        _timeLimitHours.Value = s.DailyTimeLimitMinutes / 60;
        _timeLimitMinutes.Value = s.DailyTimeLimitMinutes % 60;
        _resetTime.Value = DateTime.Today.Add(s.ResetTime.ToTimeSpan());
        _dimEnd.Value = s.DimEndPercent;
        _alertsEnabled.Checked = s.AlertsEnabled;
        _startWithWindows.Checked = s.StartWithWindows;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        var s = _settingsService.Settings;

        s.DailyTimeLimitMinutes = (int)_timeLimitHours.Value * 60 + (int)_timeLimitMinutes.Value;
        s.ResetTime = TimeOnly.FromDateTime(_resetTime.Value);
        s.DimEndPercent = (int)_dimEnd.Value;
        s.AlertsEnabled = _alertsEnabled.Checked;
        s.StartWithWindows = _startWithWindows.Checked;

        _settingsService.SaveSettings();
    }
}
