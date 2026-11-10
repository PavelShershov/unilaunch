using Microsoft.Win32;
using System.Diagnostics;

namespace UniLauncher.Modules;

public sealed class ScreenshotModule
{
    public ModuleResult Apply()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Keyboard", true);
            if (key is null)
                return new("screenshot", "Print Screen", false, "Нет доступа к пользовательским настройкам клавиатуры.");
            key.SetValue("PrintScreenKeyForSnippingEnabled", 1, RegistryValueKind.DWord);

            // Best-effort launch test: ms-screenclip is the Windows screen-clipping URI.
            Process.Start(new ProcessStartInfo("explorer.exe", "ms-screenclip:") { UseShellExecute = true });
            return new("screenshot", "Print Screen", true,
                "Включён пользовательский параметр PrintScreenKeyForSnippingEnabled и проверен запуск screen clip. Если PrtSc не изменился, проверьте доступность Snip & Sketch и политики Windows.");
        }
        catch (Exception ex)
        {
            return new("screenshot", "Print Screen", false, ex.Message);
        }
    }
}
