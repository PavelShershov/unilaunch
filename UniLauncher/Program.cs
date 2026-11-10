namespace UniLauncher;

internal static class Program
{
    private static Mutex? _instanceMutex;

    [STAThread]
    private static void Main()
    {
        _instanceMutex = new Mutex(true, @"Local\UniLauncher_v1_Instance", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("UniLauncher is already running in this Windows session.",
                "UniLauncher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => WriteFatalLog(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex) WriteFatalLog(ex);
        };

        try { Application.Run(new MainForm()); }
        finally
        {
            _instanceMutex.ReleaseMutex();
            _instanceMutex.Dispose();
        }
    }

    private static void WriteFatalLog(Exception ex)
    {
        try
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UniLauncher");
            Directory.CreateDirectory(root);
            File.AppendAllText(Path.Combine(root, "fatal.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}");
        }
        catch { }
    }
}
