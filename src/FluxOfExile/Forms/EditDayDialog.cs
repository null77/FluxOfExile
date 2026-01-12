using FluxOfExile.Models;

namespace FluxOfExile.Forms;

public class EditDayDialog : Form
{
    private readonly DateTime _dayStart;
    private readonly DateTime _dayEnd;
    private readonly DateTime _dayDate;
    private readonly double _currentMinutes;

    private Label _dayLabel = null!;
    private Label _hoursLabel = null!;
    private Label _minutesLabel = null!;
    private NumericUpDown _hoursInput = null!;
    private NumericUpDown _minutesInput = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;
    private Button _deleteButton = null!;

    public double NewMinutes { get; private set; }
    public bool DeleteRequested { get; private set; }

    public EditDayDialog(DateTime dayStart, DateTime dayEnd, DateTime dayDate, double currentMinutes)
    {
        _dayStart = dayStart;
        _dayEnd = dayEnd;
        _dayDate = dayDate;
        _currentMinutes = currentMinutes;

        InitializeControls();
        LoadCurrentValue();
    }

    private void InitializeControls()
    {
        Text = "Edit Playtime";
        ClientSize = new Size(350, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        // Day label showing which day is being edited
        _dayLabel = new Label
        {
            Location = new Point(20, 20),
            Size = new Size(310, 25),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Text = $"Editing: {_dayDate:ddd MMM dd, yyyy}"
        };
        Controls.Add(_dayLabel);

        // Hours label
        _hoursLabel = new Label
        {
            Location = new Point(20, 60),
            Size = new Size(100, 25),
            Text = "Hours:",
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(_hoursLabel);

        // Hours input
        _hoursInput = new NumericUpDown
        {
            Location = new Point(130, 60),
            Size = new Size(80, 25),
            Minimum = 0,
            Maximum = 24,
            Value = 0
        };
        Controls.Add(_hoursInput);

        // Minutes label
        _minutesLabel = new Label
        {
            Location = new Point(20, 95),
            Size = new Size(100, 25),
            Text = "Minutes:",
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(_minutesLabel);

        // Minutes input
        _minutesInput = new NumericUpDown
        {
            Location = new Point(130, 95),
            Size = new Size(80, 25),
            Minimum = 0,
            Maximum = 59,
            Value = 0
        };
        Controls.Add(_minutesInput);

        // Save button
        _saveButton = new Button
        {
            Text = "Save",
            Location = new Point(50, 150),
            Size = new Size(80, 30),
            DialogResult = DialogResult.None // Handle manually for validation
        };
        _saveButton.Click += SaveButton_Click;
        Controls.Add(_saveButton);

        // Cancel button
        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(140, 150),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(_cancelButton);

        // Delete button
        _deleteButton = new Button
        {
            Text = "Delete Day",
            Location = new Point(230, 150),
            Size = new Size(90, 30)
        };
        _deleteButton.Click += DeleteButton_Click;
        Controls.Add(_deleteButton);

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;
    }

    private void LoadCurrentValue()
    {
        var hours = (int)(_currentMinutes / 60);
        var minutes = (int)(_currentMinutes % 60);

        _hoursInput.Value = hours;
        _minutesInput.Value = minutes;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        NewMinutes = (double)_hoursInput.Value * 60 + (double)_minutesInput.Value;

        // Check if user is setting time to zero
        if (NewMinutes == 0)
        {
            var result = MessageBox.Show(
                "Set playtime to 0 minutes?\n\nThis will delete all records for this day.",
                "Confirm Zero Time",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (result != DialogResult.OK)
                return; // Cancel save
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            $"Delete all playtime for {_dayDate:ddd MMM dd}?\n\nThis cannot be undone.",
            "Delete Day",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            DeleteRequested = true;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
