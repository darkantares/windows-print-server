using System.Runtime.InteropServices;
using System.Text;

namespace LocalPrintService.Api;

/// <summary>
/// Registra/borra la entrada de "Programas y características" (Panel de control)
/// para que el servicio pueda desinstalarse desde ahí.
///
/// Se usa advapi32 directamente porque Microsoft.Win32.Registry no esta
/// disponible sin un paquete NuGet extra, y reg.exe corrompe las comillas
/// dentro del valor UninstallString (que contiene "ruta\exe" --uninstall).
/// </summary>
internal static class ControlPanelUninstaller
{
    // Clave que Panel de control lee para mostrar "Desinstalar".
    private const string UninstallSubKey =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\LocalPrintService";

    // HKEY_LOCAL_MACHINE
    private static readonly IntPtr HKEY_LOCAL_MACHINE = new(unchecked((int)0x80000002));

    private const int KEY_WRITE = 0x20006;
    private const int REG_SZ = 1;
    private const int REG_DWORD = 4;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegCreateKeyEx(
        IntPtr hKey, string subKey, int reserved, string? lpClass,
        int options, int samDesired, IntPtr securityAttributes,
        out IntPtr resultKey, out uint disposition);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegSetValueEx(
        IntPtr hKey, string? valueName, int reserved, int type,
        byte[]? data, int dataSize);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegDeleteKey(IntPtr hKey, string subKey);

    [DllImport("advapi32.dll")]
    private static extern int RegCloseKey(IntPtr hKey);

    /// <summary>
    /// Crea la entrada de desinstalación apuntando al exe instalado.
    /// Debe invocarse con privilegios de administrador.
    /// </summary>
    public static void Register(string installedExePath, long estimatedSizeKb)
    {
        var rc = RegCreateKeyEx(
            HKEY_LOCAL_MACHINE, UninstallSubKey, 0, null, 0, KEY_WRITE,
            IntPtr.Zero, out var key, out _);

        if (rc != 0)
        {
            throw new InvalidOperationException(
                $"No se pudo crear la clave de desinstalación en el Registro (código {rc}).");
        }

        try
        {
            SetString(key, "DisplayName", "Local Print Service");
            SetString(key, "Publisher", "LocalPrintService");
            SetString(key, "DisplayVersion", "1.0.0");
            // Panel de control ejecuta esto al desinstalar.
            SetString(key, "UninstallString", $"\"{installedExePath}\" --uninstall");
            SetString(key, "DisplayIcon", installedExePath);
            SetDword(key, "NoModify", 1);
            SetDword(key, "NoRepair", 1);
            SetDword(key, "EstimatedSize", (int)Math.Min(estimatedSizeKb, int.MaxValue));
        }
        finally
        {
            RegCloseKey(key);
        }
    }

    /// <summary>
    /// Elimina la entrada de Panel de control. No falla si no existía.
    /// Debe invocarse con privilegios de administrador.
    /// </summary>
    public static void Unregister()
    {
        RegDeleteKey(HKEY_LOCAL_MACHINE, UninstallSubKey);
    }

    private static void SetString(IntPtr key, string name, string value)
    {
        var data = Encoding.Unicode.GetBytes(value + "\0");
        RegSetValueEx(key, name, 0, REG_SZ, data, data.Length);
    }

    private static void SetDword(IntPtr key, string name, int value)
    {
        var data = BitConverter.GetBytes(value);
        RegSetValueEx(key, name, 0, REG_DWORD, data, data.Length);
    }
}
