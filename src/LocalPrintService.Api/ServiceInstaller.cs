using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LocalPrintService.Api;

public enum ServiceState
{
    NotInstalled,
    Installed,
    Running
}

internal static class ServiceInstaller
{
    private const string ServiceName = "LocalPrintService";
    private const string DisplayName = "Local Print Service";
    private const string Description = "Universal Print Service for Windows - Local Print Server";

    // Instalación estable en Program Files (servicio + Panel de control apuntan aquí).
    private const string InstallDirName = "LocalPrintService";
    private const string InstalledExeName = "LocalPrintService.exe";

    // MoveFileEx: borra la ruta en el próximo reinicio (para el exe en ejecución).
    private const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x1;

    public static bool Elevate(string exePath, string argument)
    {
        try
        {
            var psi = new ProcessStartInfo(exePath, argument)
            {
                Verb = "runas",
                UseShellExecute = true,
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false; // UAC cancelado o fallo al lanzar
        }
    }

    public static ServiceState GetState()
    {
        var (exitCode, output, _) = RunScRaw($"query {ServiceName}");
        if (exitCode != 0)
        {
            return ServiceState.NotInstalled;
        }

        return output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase)
            ? ServiceState.Running
            : ServiceState.Installed;
    }

    public static int Install(string exePath)
    {
        if (!IsAdministrator())
        {
            return Elevate(exePath, "--install") ? 0 : 1;
        }

        try
        {
            // 1. Copia el exe (y config/certs si los hay) a Program Files,
            //    para que el servicio y el desinstalador no dependan de la
            //    descarga original (que el usuario podría borrar).
            var installDir = GetInstallDir();
            var installedExe = InstallFiles(exePath, installDir);

            // 2. Registra el servicio apuntando a la copia instalada.
            RunSc($"stop {ServiceName}", ignoreErrors: true);
            RunSc($"delete {ServiceName}", ignoreErrors: true);
            Thread.Sleep(2000);

            RunSc($"create {ServiceName} start= auto binPath= \"{installedExe}\" DisplayName= \"{DisplayName}\"");
            RunSc($"description {ServiceName} \"{Description}\"", ignoreErrors: true);
            RunSc($"start {ServiceName}");

            // 3. Entrada de "Programas y características" para poder desinstalar
            //    desde Panel de control.
            var sizeKb = new FileInfo(installedExe).Length / 1024;
            ControlPanelUninstaller.Register(installedExe, sizeKb);

            Ui.ShowInfo(
                "El servicio de impresión local se instaló correctamente y quedó en ejecución.\n\n" +
                "Ya aparece en Panel de control > Programas para desinstalarlo.",
                "Instalación completada");
            return 0;
        }
        catch (Exception ex)
        {
            Ui.ShowError($"No se pudo instalar el servicio:\n{ex.Message}", "Local Print Service");
            return 1;
        }
    }

    public static int Uninstall(string exePath)
    {
        if (!IsAdministrator())
        {
            return Elevate(exePath, "--uninstall") ? 0 : 1;
        }

        try
        {
            RunSc($"stop {ServiceName}", ignoreErrors: true);
            Thread.Sleep(2000);
            RunSc($"delete {ServiceName}", ignoreErrors: true);

            // Quita la entrada de Panel de control.
            ControlPanelUninstaller.Unregister();

            // Limpia los archivos instalados. El exe en ejecución (este
            // proceso) no puede borrarse a sí mismo, así que se agenda su
            // borrado en el próximo reinicio; el resto se borra ahora.
            CleanupInstalledFiles(GetInstallDir(), exePath);

            Ui.ShowInfo(
                "El servicio de impresión local fue desinstalado.\n\n" +
                "Los archivos se eliminarán por completo la próxima vez que reinicie Windows.",
                "Local Print Service");
            return 0;
        }
        catch (Exception ex)
        {
            Ui.ShowError($"No se pudo desinstalar el servicio:\n{ex.Message}", "Local Print Service");
            return 1;
        }
    }

    public static int Start()
    {
        if (!IsAdministrator())
        {
            return 1;
        }

        return RunSc($"start {ServiceName}") == 0 ? 0 : 1;
    }

    public static int Stop()
    {
        if (!IsAdministrator())
        {
            return 1;
        }

        return RunSc($"stop {ServiceName}") == 0 ? 0 : 1;
    }

    private static bool IsAdministrator()
    {
        return new System.Security.Principal.WindowsPrincipal(
                System.Security.Principal.WindowsIdentity.GetCurrent())
            .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    private static string GetInstallDir()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            InstallDirName);
    }

    /// <summary>
    /// Copia el exe (self-contained single-file) y, si existen junto a él,
    /// archivos de configuración (appsettings*.json, web.config) y la carpeta
    /// de certificados. Devuelve la ruta del exe instalado.
    /// </summary>
    private static string InstallFiles(string sourceExe, string installDir)
    {
        Directory.CreateDirectory(installDir);
        var targetExe = Path.Combine(installDir, InstalledExeName);

        // Evita copiar el archivo sobre sí mismo (re-instalación desde el exe instalado).
        if (!string.Equals(Path.GetFullPath(sourceExe), Path.GetFullPath(targetExe), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(sourceExe, targetExe, overwrite: true);
        }

        var sourceDir = Path.GetDirectoryName(sourceExe) ?? installDir;
        foreach (var file in Directory.EnumerateFiles(sourceDir))
        {
            var name = Path.GetFileName(file);
            if (string.Equals(name, Path.GetFileName(sourceExe), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (name.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("web.config", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(file, Path.Combine(installDir, name), overwrite: true);
            }
        }

        // Carpeta de certificados HTTPS (si existe junto al exe origen).
        var certsSrc = Path.Combine(sourceDir, "certs");
        if (Directory.Exists(certsSrc))
        {
            var certsDest = Path.Combine(installDir, "certs");
            Directory.CreateDirectory(certsDest);
            foreach (var file in Directory.EnumerateFiles(certsSrc))
            {
                File.Copy(file, Path.Combine(certsDest, Path.GetFileName(file)), overwrite: true);
            }
        }

        return targetExe;
    }

    /// <summary>
    /// Borra los archivos instalados. El exe en ejecución (este proceso) se
    /// agenda para borrarse en el próximo reinicio con MoveFileEx.
    /// </summary>
    private static void CleanupInstalledFiles(string installDir, string runningExePath)
    {
        if (!Directory.Exists(installDir))
        {
            return;
        }

        // Subcarpetas (certs) y archivos que no sean el exe en ejecución.
        foreach (var dir in Directory.EnumerateDirectories(installDir))
        {
            try { Directory.Delete(dir, recursive: true); }
            catch { /* en uso; se borrará en el reinicio si queda vacía */ }
        }

        var installedExe = Path.Combine(installDir, InstalledExeName);
        var runningFromInstall = string.Equals(
            Path.GetFullPath(runningExePath), Path.GetFullPath(installedExe),
            StringComparison.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(installDir))
        {
            if (runningFromInstall &&
                string.Equals(Path.GetFileName(file), InstalledExeName, StringComparison.OrdinalIgnoreCase))
            {
                continue; // es este proceso; se agenda abajo
            }

            try { File.Delete(file); }
            catch { /* en uso */ }
        }

        if (runningFromInstall)
        {
            // No podemos borrarnos a nosotros mismos: se elimina al reiniciar.
            ScheduleDeleteOnReboot(installedExe);
            ScheduleDeleteOnReboot(installDir);
        }
        else
        {
            try { File.Delete(installedExe); }
            catch { ScheduleDeleteOnReboot(installedExe); }
            try { Directory.Delete(installDir, recursive: false); }
            catch { ScheduleDeleteOnReboot(installDir); }
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, int dwFlags);

    /// <summary>Agenda el borrado de una ruta (archivo o carpeta) en el próximo reinicio.</summary>
    private static void ScheduleDeleteOnReboot(string path)
    {
        // lpNewFileName = null => borra en el arranque siguiente.
        MoveFileEx(path, null, MOVEFILE_DELAY_UNTIL_REBOOT);
    }

    private static (int ExitCode, string Output, string Error) RunScRaw(string arguments)
    {
        var psi = new ProcessStartInfo("sc.exe", arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error);
    }

    private static int RunSc(string arguments, bool ignoreErrors = false)
    {
        var (exitCode, output, error) = RunScRaw(arguments);
        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output.TrimEnd());
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.Error.WriteLine(error.TrimEnd());
        }

        if (exitCode != 0 && !ignoreErrors)
        {
            throw new InvalidOperationException($"sc.exe {arguments} exited with code {exitCode}");
        }

        return exitCode;
    }
}