using FluxOfExile.Forms;

namespace FluxOfExile;

static class Program
{
    public static bool DebugMode { get; private set; }

    [STAThread]
    static void Main(string[] args)
    {
        // Check for debug mode
#if DEBUG
        DebugMode = true;
#else
        DebugMode = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);
#endif

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
