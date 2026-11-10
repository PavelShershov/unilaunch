namespace UniLauncher.Core;

public sealed class Log
{
    private readonly string _cloudPath;
    private readonly string _localPath;
    public event Action<string>? EntryAdded;

    public Log(string cloudRoot, string localRoot)
    {
        _cloudPath = Path.Combine(cloudRoot, "UniLauncher.log");
        _localPath = Path.Combine(localRoot, "UniLauncher.log");
    }

    public void Write(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        Append(_cloudPath, line);
        if (!string.Equals(_cloudPath, _localPath, StringComparison.OrdinalIgnoreCase))
            Append(_localPath, line);
        EntryAdded?.Invoke(line);
    }

    private static void Append(string path, string line)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch
        {
            // Logging must never crash the launcher.
        }
    }
}
