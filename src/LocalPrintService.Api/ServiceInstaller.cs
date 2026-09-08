using System.Diagnostics;

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
            RunSc($"stop {ServiceName}", ignoreErrors: true);
            RunSc($"delete {ServiceName}", ignoreErrors: true);
            Thread.Sleep(2000);

            RunSc($"create {ServiceName} start= auto binPath= \"{exePath}\" DisplayName= \"{DisplayName}\"");
            RunSc($"description {ServiceName} \"{Description}\"", ignoreErrors: true);
            RunSc($"start {ServiceName}");

            Ui.ShowInfo(
                "El servicio de impresión local se instaló correctamente y quedó en ejecución.",
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

            Ui.ShowInfo("El servicio de impresión local fue desinstalado.", "Local Print Service");
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