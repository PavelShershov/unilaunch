using System.Diagnostics;
using Microsoft.Win32;
using UniLauncher.Models;

namespace UniLauncher.Modules;

public sealed class BrowserModule
{
    public string? FindChrome()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe")
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    public string? FindYandex()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Yandex", "YandexBrowser", "Application", "browser.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Yandex", "YandexBrowser", "Application", "browser.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yandex", "YandexBrowser", "Application", "browser.exe")
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    public ModuleResult Launch(string? executable, IEnumerable<WebsiteItem> sites, string browserName)
    {
        var urls = sites.Where(s => s.Enabled)
            .OrderBy(s => s.Order)
            .Select(s => s.Url.Trim())
            .Where(IsHttpUrl)
            .ToList();

        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            return new("browser", browserName, false, "Исполняемый файл браузера не найден. Укажите путь вручную.");
        if (urls.Count == 0)
            return new("browser", browserName, false, "Нет включённых корректных сайтов.");

        try
        {
            var args = string.Join(" ", urls.Select(u => "\"" + u.Replace("\"", "") + "\""));
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = args,
                UseShellExecute = true
            });
            return new("browser", browserName, true, $"Отправлен запуск браузера с {urls.Count} сайтами. Авторизация зависит от доступного профиля браузера.");
        }
        catch (Exception ex)
        {
            return new("browser", browserName, false, ex.Message);
        }
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
