using UniLauncher.Core;
using UniLauncher.Models;
using UniLauncher.Modules;

namespace UniLauncher;

public sealed class MainForm : Form
{
    private readonly ConfigManager _config = new();
    private readonly AppSettings _settings;
    private readonly Log _log;
    private readonly Dictionary<string, CheckBox> _checks = new();
    private readonly ListBox _results = new();
    private readonly Label _status = new();
    private readonly TextBox _wallpaper = new();
    private readonly TextBox _chrome = new();
    private readonly TextBox _yandex = new();
    private readonly DataGridView _apps = new();
    private readonly DataGridView _sites = new();
    private readonly ComboBox _browserChoice = new();
    private readonly ComboBox _profileChoice = new();
    private List<WorkspaceProfile> _profiles = [];
    private bool _initializing = true;

    private readonly DesktopLayoutModule _desktop = new();
    private readonly WallpaperModule _wallpaperModule = new();
    private readonly KeyboardModule _keyboard = new();
    private readonly ScreenshotModule _screenshot = new();
    private readonly ApplicationModule _application = new();
    private readonly BrowserModule _browser = new();
    private readonly DiagnosticsModule _diagnostics = new();

    private static readonly (string Id, string Title, string Description)[] Modules =
    [
        ("desktop", "Desktop Organizer", "Save / restore desktop icon positions"),
        ("wallpaper", "Wallpaper Manager", "Choose and apply a desktop image"),
        ("keyboard", "Keyboard Manager", "Set Left Alt + Shift language switching"),
        ("screenshot", "Screenshot Manager", "Enable Print Screen screen snipping"),
        ("applications", "Application Manager", "Launch selected EXE files and shortcuts"),
        ("browser", "Browser Manager", "Open selected websites in Chrome or Yandex")
    ];

    public MainForm()
    {
        _settings = _config.Load();
        _log = new Log(_config.Root, _config.LocalRoot);
        Text = "UniLauncher v1.0 Release Candidate";
        MinimumSize = new Size(920, 700);
        Size = new Size(1080, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 27, 33);
        ForeColor = Color.FromArgb(235, 238, 242);
        Font = new Font("Segoe UI", 9.5F);
        BuildUi();
        RestoreChecks();
        LoadData();
        _initializing = false;
        _log.Write("UniLauncher started. Config folder: " + _config.Root);
    }

    private void BuildUi()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildHomeTab());
        tabs.TabPages.Add(BuildProgramsTab());
        tabs.TabPages.Add(BuildBrowserTab());
        tabs.TabPages.Add(BuildProfilesTab());
        tabs.TabPages.Add(BuildDiagnosticsTab());
        Controls.Add(tabs);
    }

    private TabPage NewTab(string title)
    {
        var page = new TabPage(title) { BackColor = Color.FromArgb(24, 27, 33), ForeColor = ForeColor, Padding = new Padding(14) };
        return page;
    }

    private TabPage BuildHomeTab()
    {
        var page = NewTab("Workspace");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(8) };
        var heading = new Label { Text = "UniLauncher", Font = new Font("Segoe UI Semibold", 24), ForeColor = Color.White, AutoSize = true };
        panel.Controls.Add(heading);
        panel.Controls.Add(new Label { Text = "Personal Windows Workspace · Windows 10 · No administrator elevation", ForeColor = Color.Silver, AutoSize = true, Margin = new Padding(0, 0, 0, 18) });

        foreach (var m in Modules)
        {
            var card = new Panel { Width = 960, Height = 66, BackColor = Color.FromArgb(35, 40, 49), Margin = new Padding(0, 0, 0, 8) };
            var cb = new CheckBox { Text = m.Title, AutoSize = true, Location = new Point(14, 8), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 10.5F) };
            var desc = new Label { Text = m.Description, AutoSize = true, Location = new Point(38, 34), ForeColor = Color.Silver };
            card.Controls.Add(cb); card.Controls.Add(desc); panel.Controls.Add(card);
            _checks[m.Id] = cb;
        }

        var buttons = new FlowLayoutPanel { Width = 950, Height = 52, Margin = new Padding(0, 10, 0, 8) };
        buttons.Controls.Add(MakeButton("Apply selected", ApplySelectedAsync, true));
        buttons.Controls.Add(MakeButton("Save selection", () => { SaveSettings(); SetStatus("Selection saved."); }, false));
        buttons.Controls.Add(MakeButton("Select all", () => { foreach (var c in _checks.Values) c.Checked = true; SaveSettings(); }, false));
        buttons.Controls.Add(MakeButton("Select none", () => { foreach (var c in _checks.Values) c.Checked = false; SaveSettings(); }, false));
        buttons.Controls.Add(MakeButton("Remember desktop layout", CaptureDesktopLayout, false));
        panel.Controls.Add(buttons);

        var paths = new GroupBox { Text = "Shared resources", ForeColor = Color.White, Width = 950, Height = 150, Padding = new Padding(10) };
        AddPathRow(paths, "Wallpaper", _wallpaper, "Browse", 24, SelectWallpaper);
        AddPathRow(paths, "Chrome EXE", _chrome, "Browse", 62, () => SelectExecutable(_chrome, "Chrome executable"));
        AddPathRow(paths, "Yandex EXE", _yandex, "Browse", 100, () => SelectExecutable(_yandex, "Yandex executable"));
        panel.Controls.Add(paths);
        _status.AutoSize = true; _status.ForeColor = Color.LightSteelBlue; _status.Margin = new Padding(0, 10, 0, 0);
        panel.Controls.Add(_status);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildProgramsTab()
    {
        var page = NewTab("Applications");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        StyleGrid(_apps);
        _apps.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Run", DataPropertyName = "Enabled", Width = 55 });
        _apps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "Name", Width = 180 });
        _apps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Path", DataPropertyName = "Path", Width = 500 });
        _apps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Arguments", DataPropertyName = "Arguments", Width = 180 });
        layout.Controls.Add(_apps, 0, 0);
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        bar.Controls.Add(MakeButton("Add EXE / shortcut", AddApplication, false));
        bar.Controls.Add(MakeButton("Remove selected", RemoveApplication, false));
        bar.Controls.Add(MakeButton("Save list", SaveSettings, false));
        layout.Controls.Add(bar, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildBrowserTab()
    {
        var page = NewTab("Browser & Websites");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        StyleGrid(_sites);
        _sites.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Open", DataPropertyName = "Enabled", Width = 55 });
        _sites.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "Name", Width = 220 });
        _sites.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "URL", DataPropertyName = "Url", Width = 570 });
        _sites.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Order", DataPropertyName = "Order", Width = 70 });
        layout.Controls.Add(_sites, 0, 0);
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _browserChoice.DropDownStyle = ComboBoxStyle.DropDownList;
        _browserChoice.Width = 150;
        _browserChoice.Items.AddRange(["Chrome", "Yandex", "Both"]);
        _browserChoice.SelectedIndex = 2;
        _browserChoice.SelectedIndexChanged += (_, _) =>
        {
            if (_initializing) return;
            _settings.PreferredBrowser = _browserChoice.SelectedIndex switch { 0 => "chrome", 1 => "yandex", _ => "both" };
            SaveSettings();
        };
        bar.Controls.Add(_browserChoice);
        bar.Controls.Add(MakeButton("Add website", AddWebsite, false));
        bar.Controls.Add(MakeButton("Remove selected", RemoveWebsite, false));
        bar.Controls.Add(MakeButton("Save list", SaveSettings, false));
        bar.Controls.Add(MakeButton("Test Chrome", () => LaunchBrowser("chrome"), false));
        bar.Controls.Add(MakeButton("Test Yandex", () => LaunchBrowser("yandex"), false));
        layout.Controls.Add(bar, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildProfilesTab()
    {
        var page = NewTab("Profiles");
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12),
            AutoScroll = true
        };
        panel.Controls.Add(new Label
        {
            Text = "Workspace profiles",
            Font = new Font("Segoe UI Semibold", 20),
            ForeColor = Color.White,
            AutoSize = true
        });
        panel.Controls.Add(new Label
        {
            Text = "Save combinations of modules, applications and websites. Profiles are stored in the network folder and local backup.",
            ForeColor = Color.Silver,
            AutoSize = true,
            MaximumSize = new Size(850, 0),
            Margin = new Padding(0, 0, 0, 18)
        });

        _profileChoice.DropDownStyle = ComboBoxStyle.DropDownList;
        _profileChoice.Width = 400;
        _profileChoice.BackColor = Color.FromArgb(35, 40, 49);
        _profileChoice.ForeColor = Color.White;
        panel.Controls.Add(_profileChoice);

        var buttons = new FlowLayoutPanel { Width = 900, Height = 55 };
        buttons.Controls.Add(MakeButton("Apply profile", ApplySelectedProfile, true));
        buttons.Controls.Add(MakeButton("Save current as new", SaveNewProfile, false));
        buttons.Controls.Add(MakeButton("Update selected", UpdateSelectedProfile, false));
        buttons.Controls.Add(MakeButton("Delete selected", DeleteSelectedProfile, false));
        panel.Controls.Add(buttons);

        panel.Controls.Add(new Label
        {
            Text = "A profile stores the module checkboxes and which listed applications/websites are enabled. It does not store passwords or browser session tokens.",
            ForeColor = Color.LightSteelBlue,
            AutoSize = true,
            MaximumSize = new Size(850, 0)
        });
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildDiagnosticsTab()
    {
        var page = NewTab("Diagnostics");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        _results.Dock = DockStyle.Fill;
        _results.BackColor = Color.FromArgb(17, 20, 25);
        _results.ForeColor = Color.Gainsboro;
        layout.Controls.Add(_results, 0, 0);
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        bar.Controls.Add(MakeButton("Run diagnostics", RunDiagnostics, false));
        bar.Controls.Add(MakeButton("Open log folder", () => System.Diagnostics.Process.Start("explorer.exe", _config.Root), false));
        bar.Controls.Add(MakeButton("Open local backup", () => System.Diagnostics.Process.Start("explorer.exe", _config.LocalRoot), false));
        bar.Controls.Add(MakeButton("Clear view", () => _results.Items.Clear(), false));
        layout.Controls.Add(bar, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private void AddPathRow(Control parent, string label, TextBox box, string buttonText, int y, Action action)
    {
        var l = new Label { Text = label, ForeColor = Color.Gainsboro, Location = new Point(12, y + 4), Width = 95 };
        box.Location = new Point(110, y); box.Width = 690; box.BackColor = Color.FromArgb(22, 25, 31); box.ForeColor = Color.White;
        var b = new Button { Text = buttonText, Location = new Point(810, y - 1), Width = 100 };
        b.Click += (_, _) => action();
        parent.Controls.Add(l); parent.Controls.Add(box); parent.Controls.Add(b);
    }

    private static void StyleGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Color.FromArgb(24, 27, 33);
        grid.ForeColor = Color.Black;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 52, 64);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AutoGenerateColumns = false;
    }

    private Button MakeButton(string text, Action action, bool primary)
    {
        var b = new Button
        {
            Text = text, Width = primary ? 170 : 145, Height = 38,
            BackColor = primary ? Color.FromArgb(45, 112, 220) : Color.FromArgb(55, 63, 76),
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0)
        };
        b.FlatAppearance.BorderSize = 0;
        b.Click += (_, _) => action();
        return b;
    }

    private Button MakeButton(string text, Func<Task> action, bool primary)
    {
        var b = new Button
        {
            Text = text, Width = primary ? 170 : 145, Height = 38,
            BackColor = primary ? Color.FromArgb(45, 112, 220) : Color.FromArgb(55, 63, 76),
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0)
        };
        b.FlatAppearance.BorderSize = 0;
        b.Click += async (_, _) => await action();
        return b;
    }

    private void RestoreChecks()
    {
        foreach (var (id, check) in _checks)
            check.Checked = _settings.LastSelectedModules.TryGetValue(id, out var enabled) && enabled;
    }

    private void LoadData()
    {
        _wallpaper.Text = _settings.WallpaperPath ?? "";
        _chrome.Text = _settings.ChromePath ?? _browser.FindChrome() ?? "";
        _yandex.Text = _settings.YandexPath ?? _browser.FindYandex() ?? "";
        _browserChoice.SelectedIndex = _settings.PreferredBrowser switch { "chrome" => 0, "yandex" => 1, _ => 2 };
        _apps.DataSource = new BindingSource { DataSource = _settings.LaunchItems };
        _sites.DataSource = new BindingSource { DataSource = _settings.Websites };
        _profiles = _config.LoadProfiles();
        RefreshProfiles();
    }

    private void SaveSettings()
    {
        foreach (var (id, check) in _checks) _settings.LastSelectedModules[id] = check.Checked;
        _settings.WallpaperPath = _wallpaper.Text.Trim();
        _settings.ChromePath = _chrome.Text.Trim();
        _settings.YandexPath = _yandex.Text.Trim();
        try
        {
            _apps.EndEdit();
            _sites.EndEdit();
            _settings.LaunchItems = ((BindingSource)_apps.DataSource!).List.Cast<LaunchItem>().ToList();
            _settings.Websites = ((BindingSource)_sites.DataSource!).List.Cast<WebsiteItem>().ToList();
        }
        catch { }
        try
        {
            _config.Save(_settings);
            _log.Write("Settings saved.");
        }
        catch (Exception ex) { AddResult("CONFIG", false, ex.Message); }
    }

    private async Task ApplySelectedAsync()
    {
        SaveSettings();
        _results.Items.Clear();
        foreach (var m in Modules)
        {
            if (!_checks[m.Id].Checked) continue;
            ModuleResult result;
            try
            {
                result = m.Id switch
                {
                    "desktop" => _desktop.Restore(_settings),
                    "wallpaper" => _wallpaperModule.Apply(_settings.WallpaperPath),
                    "keyboard" => _keyboard.ApplyLeftAltShift(),
                    "screenshot" => _screenshot.Apply(),
                    "applications" => LaunchApplicationsResult(),
                    "browser" => LaunchBrowserResult(),
                    _ => new ModuleResult(m.Id, m.Title, false, "Unknown module.")
                };
                AddResult(result.Name, result.Success, result.Message);
            }
            catch (Exception ex) { AddResult(m.Title, false, ex.Message); }
            await Task.Delay(120);
        }
        SetStatus("Finished. Review Diagnostics for results.");
    }


    private ModuleResult LaunchApplicationsResult()
    {
        var results = _application.Launch(_settings.LaunchItems);
        foreach (var r in results) AddResult(r.Name, r.Success, r.Message);
        return new("applications", "Программы",
            results.Count > 0 && results.All(x => x.Success),
            $"Выбрано: {results.Count}; успешно: {results.Count(x => x.Success)}; ошибок: {results.Count(x => !x.Success)}.");
    }

    private void CaptureDesktopLayout()
    {
        SaveSettings();
        var result = _desktop.Capture(_settings);
        try { _config.Save(_settings); } catch (Exception ex) { AddResult("Конфигурация", false, ex.Message); }
        AddResult(result.Name, result.Success, result.Message);
    }

    private ModuleResult LaunchBrowserResult()
    {
        var pref = _settings.PreferredBrowser;
        var count = 0;
        var messages = new List<string>();
        if (pref is "chrome" or "both")
        {
            var r = _browser.Launch(_settings.ChromePath, _settings.Websites, "Google Chrome");
            messages.Add(r.Message); if (r.Success) count++;
        }
        if (pref is "yandex" or "both")
        {
            var r = _browser.Launch(_settings.YandexPath, _settings.Websites, "Yandex Browser");
            messages.Add(r.Message); if (r.Success) count++;
        }
        return new("browser", "Браузеры", count > 0, string.Join(" | ", messages));
    }

    private void LaunchBrowser(string browser)
    {
        SaveSettings();
        var path = browser == "chrome" ? _settings.ChromePath : _settings.YandexPath;
        var r = _browser.Launch(path, _settings.Websites, browser == "chrome" ? "Google Chrome" : "Yandex Browser");
        AddResult(r.Name, r.Success, r.Message);
    }

    private void RefreshProfiles(string? selectName = null)
    {
        _profileChoice.Items.Clear();
        foreach (var profile in _profiles) _profileChoice.Items.Add(profile.Name);
        if (_profileChoice.Items.Count > 0)
        {
            var index = selectName is null ? 0 : _profiles.FindIndex(x => x.Name == selectName);
            _profileChoice.SelectedIndex = index >= 0 ? index : 0;
        }
    }

    private WorkspaceProfile? SelectedProfile =>
        _profileChoice.SelectedIndex >= 0 && _profileChoice.SelectedIndex < _profiles.Count
            ? _profiles[_profileChoice.SelectedIndex] : null;

    private WorkspaceProfile CaptureCurrentProfile(string name)
    {
        SaveSettings();
        return new WorkspaceProfile
        {
            Name = name,
            Modules = _checks.ToDictionary(x => x.Key, x => x.Value.Checked),
            ApplicationPaths = _settings.LaunchItems.Where(x => x.Enabled).Select(x => x.Path).ToList(),
            WebsiteUrls = _settings.Websites.Where(x => x.Enabled).Select(x => x.Url).ToList()
        };
    }

    private void SaveNewProfile()
    {
        using var dialog = new TextPromptDialog("New profile", "Profile name:");
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var name = dialog.Value.Trim();
        if (name.Length == 0) return;
        if (_profiles.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("A profile with this name already exists.", "UniLauncher");
            return;
        }
        _profiles.Add(CaptureCurrentProfile(name));
        SaveProfilesAndRefresh(name);
    }

    private void UpdateSelectedProfile()
    {
        var selected = SelectedProfile;
        if (selected is null) return;
        var updated = CaptureCurrentProfile(selected.Name);
        var index = _profiles.IndexOf(selected);
        _profiles[index] = updated;
        SaveProfilesAndRefresh(updated.Name);
    }

    private void ApplySelectedProfile()
    {
        var profile = SelectedProfile;
        if (profile is null) return;

        foreach (var (id, check) in _checks)
            check.Checked = profile.Modules.TryGetValue(id, out var enabled) && enabled;

        var appPaths = new HashSet<string>(profile.ApplicationPaths, StringComparer.OrdinalIgnoreCase);
        foreach (var app in _settings.LaunchItems) app.Enabled = appPaths.Contains(app.Path);

        var urls = new HashSet<string>(profile.WebsiteUrls, StringComparer.OrdinalIgnoreCase);
        foreach (var site in _settings.Websites) site.Enabled = urls.Contains(site.Url);

        RefreshApps();
        RefreshSites();
        SaveSettings();
        AddResult("Profiles", true, $"Profile '{profile.Name}' applied.");
        SetStatus($"Profile '{profile.Name}' applied.");
    }

    private void DeleteSelectedProfile()
    {
        var profile = SelectedProfile;
        if (profile is null) return;
        if (MessageBox.Show($"Delete profile '{profile.Name}'?", "UniLauncher",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _profiles.Remove(profile);
        SaveProfilesAndRefresh();
    }

    private void SaveProfilesAndRefresh(string? selectName = null)
    {
        try
        {
            _config.SaveProfiles(_profiles);
            RefreshProfiles(selectName);
            AddResult("Profiles", true, "Profiles saved to network folder/local backup.");
        }
        catch (Exception ex) { AddResult("Profiles", false, ex.Message); }
    }

    private void RunDiagnostics()
    {
        SaveSettings();
        _results.Items.Clear();
        foreach (var result in _diagnostics.Run(_config.Root, _settings.ChromePath, _settings.YandexPath))
            AddResult(result.Name, result.Success, result.Message);
        AddResult("Local backup", Directory.Exists(_config.LocalRoot), _config.LocalRoot);
    }

    private void AddApplication()
    {
        using var dlg = new OpenFileDialog { Filter = "Applications and shortcuts|*.exe;*.lnk|All files|*.*" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        _settings.LaunchItems.Add(new LaunchItem { Name = Path.GetFileNameWithoutExtension(dlg.FileName), Path = dlg.FileName, Enabled = true });
        RefreshApps();
    }

    private void RemoveApplication()
    {
        if (_apps.CurrentRow?.DataBoundItem is LaunchItem item) _settings.LaunchItems.Remove(item);
        RefreshApps();
    }

    private void AddWebsite()
    {
        using var dlg = new WebsiteDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _settings.Websites.Add(new WebsiteItem { Name = dlg.SiteName, Url = dlg.SiteUrl, Enabled = true, Order = _settings.Websites.Count });
            RefreshSites();
        }
    }

    private void RemoveWebsite()
    {
        if (_sites.CurrentRow?.DataBoundItem is WebsiteItem item) _settings.Websites.Remove(item);
        RefreshSites();
    }

    private void RefreshApps() => _apps.DataSource = new BindingSource { DataSource = _settings.LaunchItems };
    private void RefreshSites() => _sites.DataSource = new BindingSource { DataSource = _settings.Websites };

    private void SelectWallpaper()
    {
        using var dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*" };
        if (dlg.ShowDialog() == DialogResult.OK) _wallpaper.Text = dlg.FileName;
    }

    private void SelectExecutable(TextBox target, string title)
    {
        using var dlg = new OpenFileDialog { Title = title, Filter = "Executable|*.exe|All files|*.*" };
        if (dlg.ShowDialog() == DialogResult.OK) target.Text = dlg.FileName;
    }

    private void AddResult(string name, bool success, string message)
    {
        var line = $"{(success ? "[OK]" : "[ERROR]")} {name}: {message}";
        _results.Items.Add(line);
        _log.Write(line);
    }

    private void SetStatus(string message) => _status.Text = message;
}

internal sealed class WebsiteDialog : Form
{
    private readonly TextBox _name = new() { Width = 360 };
    private readonly TextBox _url = new() { Width = 360 };
    public string SiteName => _name.Text.Trim();
    public string SiteUrl => _url.Text.Trim();

    public WebsiteDialog()
    {
        Text = "Add website"; Size = new Size(440, 220); StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(24, 27, 33); ForeColor = Color.White;
        var l1 = new Label { Text = "Name", Location = new Point(20, 20), AutoSize = true };
        _name.Location = new Point(20, 45);
        var l2 = new Label { Text = "URL (https://...)", Location = new Point(20, 80), AutoSize = true };
        _url.Location = new Point(20, 105);
        var ok = new Button { Text = "Add", Location = new Point(285, 145), DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Location = new Point(365, 145), DialogResult = DialogResult.Cancel };
        Controls.AddRange([l1, _name, l2, _url, ok, cancel]);
        AcceptButton = ok; CancelButton = cancel;
        FormClosing += (_, e) =>
        {
            if (DialogResult == DialogResult.OK &&
                (string.IsNullOrWhiteSpace(SiteName) || !Uri.TryCreate(SiteUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != "https" && uri.Scheme != "http")))
            {
                MessageBox.Show("Enter a name and a valid http/https URL.");
                e.Cancel = true;
            }
        };
    }
}


internal sealed class TextPromptDialog : Form
{
    private readonly TextBox _input = new() { Width = 330 };
    public string Value => _input.Text;

    public TextPromptDialog(string title, string prompt)
    {
        Text = title;
        Size = new Size(410, 175);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(24, 27, 33);
        ForeColor = Color.White;
        Controls.Add(new Label { Text = prompt, Location = new Point(18, 18), AutoSize = true });
        _input.Location = new Point(18, 48);
        Controls.Add(_input);
        var ok = new Button { Text = "Save", Location = new Point(190, 88), DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Location = new Point(275, 88), DialogResult = DialogResult.Cancel };
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
