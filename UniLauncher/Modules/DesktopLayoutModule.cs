using System.Runtime.InteropServices;
using System.Text;
using UniLauncher.Models;

namespace UniLauncher.Modules;

// Uses the desktop's Shell ListView. This is Windows-version and policy sensitive.
// The module reports limitations rather than changing files or bypassing restrictions.
public sealed class DesktopLayoutModule
{
    private const uint LVM_FIRST = 0x1000;
    private const uint LVM_GETITEMCOUNT = LVM_FIRST + 4;
    private const uint LVM_GETITEMPOSITION = LVM_FIRST + 16;
    private const uint LVM_SETITEMPOSITION32 = LVM_FIRST + 49;
    private const uint PROCESS_VM_OPERATION = 0x0008;
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_VM_WRITE = 0x0020;
    private const uint MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000, MEM_RELEASE = 0x8000;
    private const uint PAGE_READWRITE = 0x04;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string? windowName);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? windowName);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, uint processId);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, uint size, uint allocationType, uint protect);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualFreeEx(IntPtr process, IntPtr address, uint size, uint freeType);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr process, IntPtr address, out POINT buffer, uint size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr process, IntPtr address, ref POINT buffer, uint size, out IntPtr written);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static IntPtr GetDesktopListView()
    {
        var progman = FindWindow("Progman", null);
        var defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (defView == IntPtr.Zero)
        {
            // Windows may host the desktop view in a WorkerW window.
            var worker = IntPtr.Zero;
            while ((worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null)) != IntPtr.Zero)
            {
                defView = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero) break;
            }
        }
        return defView == IntPtr.Zero ? IntPtr.Zero : FindWindowEx(defView, IntPtr.Zero, "SysListView32", "FolderView");
    }

    public ModuleResult Capture(AppSettings settings)
    {
        try
        {
            var list = GetDesktopListView();
            if (list == IntPtr.Zero) return new("desktop", "Рабочий стол", false, "Не найден список значков рабочего стола Windows.");
            GetWindowThreadProcessId(list, out var pid);
            var process = OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE, false, pid);
            if (process == IntPtr.Zero) return new("desktop", "Рабочий стол", false, "Windows не разрешила доступ к процессу рабочего стола.");
            try
            {
                var remote = VirtualAllocEx(process, IntPtr.Zero, 8, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (remote == IntPtr.Zero) return new("desktop", "Рабочий стол", false, "Не удалось выделить память для чтения позиций.");
                try
                {
                    var count = (int)SendMessage(list, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
                    var result = new List<DesktopPosition>();
                    for (int i = 0; i < count; i++)
                    {
                        if (SendMessage(list, LVM_GETITEMPOSITION, (IntPtr)i, remote) == IntPtr.Zero) continue;
                        if (!ReadProcessMemory(process, remote, out var point, 8, out _)) continue;
                        result.Add(new DesktopPosition { Identity = $"index:{i}", DisplayName = $"Icon {i + 1}", X = point.X, Y = point.Y });
                    }
                    settings.DesktopLayout = result;
                    settings.DesktopScreenWidth = Screen.PrimaryScreen?.Bounds.Width ?? 0;
                    settings.DesktopScreenHeight = Screen.PrimaryScreen?.Bounds.Height ?? 0;
                    return new("desktop", "Рабочий стол", true, $"Сохранено позиций: {result.Count}. В этой Beta позиции связаны с индексами значков; при изменении состава значков сопоставление может отличаться.");
                }
                finally { VirtualFreeEx(process, remote, 0, MEM_RELEASE); }
            }
            finally { CloseHandle(process); }
        }
        catch (Exception ex) { return new("desktop", "Рабочий стол", false, ex.Message); }
    }

    public ModuleResult Restore(AppSettings settings)
    {
        try
        {
            if (settings.DesktopLayout.Count == 0)
                return new("desktop", "Рабочий стол", false, "Сначала сохраните расположение значков.");
            var currentWidth = Screen.PrimaryScreen?.Bounds.Width ?? 0;
            var currentHeight = Screen.PrimaryScreen?.Bounds.Height ?? 0;
            if (settings.DesktopScreenWidth > 0 && settings.DesktopScreenHeight > 0 &&
                (settings.DesktopScreenWidth != currentWidth || settings.DesktopScreenHeight != currentHeight))
                return new("desktop", "Рабочий стол", false,
                    $"Разрешение отличается от сохранённого ({settings.DesktopScreenWidth}x{settings.DesktopScreenHeight} → {currentWidth}x{currentHeight}). Восстановление отменено во избежание неправильной раскладки.");
            var list = GetDesktopListView();
            if (list == IntPtr.Zero) return new("desktop", "Рабочий стол", false, "Не найден список значков рабочего стола.");
            GetWindowThreadProcessId(list, out var pid);
            var process = OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_WRITE, false, pid);
            if (process == IntPtr.Zero) return new("desktop", "Рабочий стол", false, "Windows не разрешила перемещение значков.");
            try
            {
                var remote = VirtualAllocEx(process, IntPtr.Zero, 8, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (remote == IntPtr.Zero) return new("desktop", "Рабочий стол", false, "Не удалось выделить память для восстановления.");
                try
                {
                    var currentCount = (int)SendMessage(list, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
                    int moved = 0;
                    foreach (var pos in settings.DesktopLayout)
                    {
                        if (!pos.Identity.StartsWith("index:")) continue;
                        if (!int.TryParse(pos.Identity.AsSpan(6), out var index)) continue;
                        if (index < 0 || index >= currentCount) continue;
                        var point = new POINT { X = pos.X, Y = pos.Y };
                        if (!WriteProcessMemory(process, remote, ref point, 8, out _)) continue;
                        SendMessage(list, LVM_SETITEMPOSITION32, (IntPtr)index, remote);
                        moved++;
                    }
                    return new("desktop", "Рабочий стол", moved > 0,
                        $"Команда восстановления отправлена для {moved} из {settings.DesktopLayout.Count} сохранённых значков (сейчас обнаружено {currentCount}). Идентификация пока индексная — обязательно проверьте результат.");
                }
                finally { VirtualFreeEx(process, remote, 0, MEM_RELEASE); }
            }
            finally { CloseHandle(process); }
        }
        catch (Exception ex) { return new("desktop", "Рабочий стол", false, ex.Message); }
    }
}
