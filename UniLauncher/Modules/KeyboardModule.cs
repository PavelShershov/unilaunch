using Microsoft.Win32;

namespace UniLauncher.Modules;

public sealed class KeyboardModule
{
    // HKCU\Keyboard Layout\Toggle: "1" = Left Alt + Shift.
    public ModuleResult ApplyLeftAltShift()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Keyboard Layout\Toggle", true);
            if (key is null)
                return new("keyboard", "Раскладка", false, "Не удалось открыть пользовательские настройки реестра.");
            key.SetValue("Hotkey", "1", RegistryValueKind.String);
            key.SetValue("Language Hotkey", "1", RegistryValueKind.String);
            key.SetValue("Layout Hotkey", "3", RegistryValueKind.String);
            return new("keyboard", "Раскладка", true,
                "Пользовательские параметры записаны. Windows может применить их после выхода и повторного входа; доменная политика может переопределить настройку.");
        }
        catch (Exception ex)
        {
            return new("keyboard", "Раскладка", false, ex.Message);
        }
    }
}
