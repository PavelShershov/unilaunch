namespace UniLauncher.Models;

public sealed class AppSettings
{
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public string PreferredBrowser { get; set; } = "both";
    public string? WallpaperPath { get; set; }
    public string? ChromePath { get; set; }
    public string? YandexPath { get; set; }
    public string? CloudRoot { get; set; }
    public Dictionary<string, bool> LastSelectedModules { get; set; } = new()
    {
        ["desktop"] = true, ["wallpaper"] = true, ["keyboard"] = true,
        ["screenshot"] = true, ["applications"] = true, ["browser"] = true
    };
    public List<LaunchItem> LaunchItems { get; set; } = [];
    public List<WebsiteItem> Websites { get; set; } = [];
    public List<DesktopPosition> DesktopLayout { get; set; } = [];
    public int DesktopScreenWidth { get; set; } = 0;
    public int DesktopScreenHeight { get; set; } = 0;
}

public sealed class LaunchItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Arguments { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

public sealed class WebsiteItem
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
}

public sealed class DesktopPosition
{
    public string Identity { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
}


public sealed class WorkspaceProfile
{
    public string Name { get; set; } = "Новый набор";
    public Dictionary<string, bool> Modules { get; set; } = new();
    public List<string> ApplicationPaths { get; set; } = [];
    public List<string> WebsiteUrls { get; set; } = [];
}


public sealed class ProfileStore
{
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public List<WorkspaceProfile> Profiles { get; set; } = [];
}
