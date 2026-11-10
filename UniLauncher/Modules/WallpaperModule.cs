using System.Runtime.InteropServices;

namespace UniLauncher.Modules;

public sealed class WallpaperModule
{
    private const uint SPI_SETDESKWALLPAPER = 0x0014;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDCHANGE = 0x02;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, string value, uint flags);

    public ModuleResult Apply(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new("wallpaper", "Обои", false, "Файл обоев не выбран или недоступен.");
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".bmp" or ".png"))
            return new("wallpaper", "Обои", false, "Выберите JPG, JPEG, BMP или PNG.");
        var ok = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path,
            SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        return ok
            ? new("wallpaper", "Обои", true, "Обои установлены.")
            : new("wallpaper", "Обои", false, $"Windows отказала в изменении обоев. Код: {Marshal.GetLastWin32Error()}");
    }
}
