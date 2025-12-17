using FluxOfExile.Forms;

namespace FluxOfExile;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Single instance check
        using var mutex = new Mutex(true, "FluxOfExile_SingleInstance", out bool isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show("FluxOfExile is already running.", "FluxOfExile",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
