using System.Text.Json;
using UniLauncher.Models;

namespace UniLauncher.Core;

public sealed class ConfigManager
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    public string CloudRoot { get; }
    public string LocalRoot { get; }
    public string Root => CloudRoot;
    public string SettingsPath => Path.Combine(CloudRoot, "settings.json");
    public string ProfilesPath => Path.Combine(CloudRoot, "profiles.json");

    private string LocalSettingsPath => Path.Combine(LocalRoot, "settings.json");
    private string LocalProfilesPath => Path.Combine(LocalRoot, "profiles.json");

    public ConfigManager()
    {
        // The executable is intended to live in the user's university network/cloud folder.
        CloudRoot = AppContext.BaseDirectory;
        LocalRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UniLauncher");
        Directory.CreateDirectory(LocalRoot);
    }

    public AppSettings Load()
    {
        var cloud = TryLoad<AppSettings>(SettingsPath);
        var local = TryLoad<AppSettings>(LocalSettingsPath);

        // Use the newest known copy. This prevents an older network copy from
        // overwriting a newer local fallback after a temporary share outage.
        var chosen = new[] { cloud, local }
            .Where(x => x is not null)
            .OrderByDescending(x => x!.UpdatedUtc)
            .FirstOrDefault();

        if (chosen is null)
            return new AppSettings();

        var json = JsonSerializer.Serialize(chosen, _options);
        try { AtomicWrite(SettingsPath, json); } catch { }
        try { AtomicWrite(LocalSettingsPath, json); } catch { }
        return chosen;
    }

    public void Save(AppSettings settings)
    {
        settings.UpdatedUtc = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(settings, _options);
        Exception? cloudError = null;

        try { AtomicWrite(SettingsPath, json); }
        catch (Exception ex) { cloudError = ex; }

        // Keep a local recovery copy even when the network share is unavailable.
        try { AtomicWrite(LocalSettingsPath, json); }
        catch (Exception localError)
        {
            if (cloudError is not null)
                throw new IOException($"Cloud and local saves failed. Cloud: {cloudError.Message}; local: {localError.Message}");
            throw;
        }

        if (cloudError is not null)
            throw new IOException($"Cloud save failed; local backup was saved. {cloudError.Message}");
    }

    public List<WorkspaceProfile> LoadProfiles()
    {
        var cloud = TryLoadProfileStore(ProfilesPath);
        var local = TryLoadProfileStore(LocalProfilesPath);
        var chosen = new[] { cloud, local }
            .Where(x => x is not null)
            .OrderByDescending(x => x!.UpdatedUtc)
            .FirstOrDefault() ?? new ProfileStore();

        var json = JsonSerializer.Serialize(chosen, _options);
        try { AtomicWrite(ProfilesPath, json); } catch { }
        try { AtomicWrite(LocalProfilesPath, json); } catch { }
        return chosen.Profiles;
    }

    public void SaveProfiles(List<WorkspaceProfile> profiles)
    {
        var store = new ProfileStore { UpdatedUtc = DateTime.UtcNow, Profiles = profiles };
        var json = JsonSerializer.Serialize(store, _options);
        Exception? cloudError = null;
        try { AtomicWrite(ProfilesPath, json); }
        catch (Exception ex) { cloudError = ex; }

        AtomicWrite(LocalProfilesPath, json);
        if (cloudError is not null)
            throw new IOException($"Profiles saved locally, but cloud save failed. {cloudError.Message}");
    }

    private ProfileStore? TryLoadProfileStore(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                // Backward compatibility with the earlier Beta's raw JSON array.
                var oldProfiles = JsonSerializer.Deserialize<List<WorkspaceProfile>>(document.RootElement.GetRawText(), _options) ?? [];
                return new ProfileStore
                {
                    UpdatedUtc = File.GetLastWriteTimeUtc(path),
                    Profiles = oldProfiles
                };
            }
            return JsonSerializer.Deserialize<ProfileStore>(document.RootElement.GetRawText(), _options);
        }
        catch { return null; }
    }

    private T? TryLoad<T>(string path)
    {
        try
        {
            if (!File.Exists(path)) return default;
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), _options);
        }
        catch { return default; }
    }

    private static void AtomicWrite(string path, string content)
    {
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temp, content);
        File.Move(temp, path, true);
    }

    private static void TryMirror(string source, string destination)
    {
        try { File.Copy(source, destination, true); } catch { }
    }
}
