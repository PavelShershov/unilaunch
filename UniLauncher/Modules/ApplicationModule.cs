using System.Diagnostics;
using UniLauncher.Models;

namespace UniLauncher.Modules;

public sealed class ApplicationModule
{
    public List<ModuleResult> Launch(IEnumerable<LaunchItem> items)
    {
        var results = new List<ModuleResult>();
        foreach (var item in items.Where(x => x.Enabled))
        {
            try
            {
                if (!File.Exists(item.Path))
                {
                    results.Add(new("applications", item.Name, false, $"Файл не найден: {item.Path}"));
                    continue;
                }

                if (Path.GetExtension(item.Path).Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
                    IsAlreadyRunning(item.Path))
                {
                    results.Add(new("applications", item.Name, true, "Программа уже запущена; повторный запуск пропущен."));
                    continue;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = item.Path,
                    Arguments = item.Arguments ?? "",
                    WorkingDirectory = Path.GetDirectoryName(item.Path) ?? AppContext.BaseDirectory,
                    UseShellExecute = true
                };
                Process.Start(psi);
                results.Add(new("applications", item.Name, true, "Запуск отправлен Windows."));
            }
            catch (Exception ex)
            {
                results.Add(new("applications", item.Name, false, ex.Message));
            }
        }
        return results;
    }

    private static bool IsAlreadyRunning(string executablePath)
    {
        var processName = Path.GetFileNameWithoutExtension(executablePath);
        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                if (string.Equals(process.MainModule?.FileName, executablePath,
                    StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch
            {
                // Windows may deny reading another process. In that case do not
                // assume it is the same executable.
            }
            finally { process.Dispose(); }
        }
        return false;
    }
}
