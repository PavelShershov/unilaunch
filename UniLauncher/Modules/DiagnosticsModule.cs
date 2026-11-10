using System.Diagnostics;
using UniLauncher.Core;

namespace UniLauncher.Modules;

public sealed class DiagnosticsModule
{
    public List<ModuleResult> Run(string configRoot, string? chromePath, string? yandexPath)
    {
        var results = new List<ModuleResult>();
        results.Add(new("diagnostics", "Windows", true,
            $"{Environment.OSVersion.VersionString}; {Environment.Is64BitOperatingSystem switch { true => "64-bit", false => "32-bit" }} OS; {Screen.PrimaryScreen?.Bounds.Width}x{Screen.PrimaryScreen?.Bounds.Height}"));

        results.Add(CheckDirectory(configRoot));
        results.Add(CheckExecutable("Google Chrome", chromePath));
        results.Add(CheckExecutable("Yandex Browser", yandexPath));
        results.Add(new("diagnostics", "Screen clipping", true,
            "URI ms-screenclip: will be tested when Screenshot Manager is applied."));

        return results;
    }

    private static ModuleResult CheckDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var test = Path.Combine(path, ".unilauncher-write-test-" + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(test, "ok");
            File.Delete(test);
            return new("diagnostics", "Cloud/config folder", true, $"Write access confirmed: {path}");
        }
        catch (Exception ex)
        {
            return new("diagnostics", "Cloud/config folder", false, $"No write access: {ex.Message}");
        }
    }

    private static ModuleResult CheckExecutable(string name, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new("diagnostics", name, false, "Executable path is not configured or browser was not found.");
        return File.Exists(path)
            ? new("diagnostics", name, true, path)
            : new("diagnostics", name, false, $"File not found: {path}");
    }
}
