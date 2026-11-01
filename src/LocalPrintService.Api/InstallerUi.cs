namespace LocalPrintService.Api;

internal static class InstallerUi
{
    public static int Run(string exePath)
    {
        var state = ServiceInstaller.GetState();

        if (state == ServiceState.Running)
        {
            Ui.ShowInfo(
                "El servicio de impresión local ya está instalado y en ejecución.\n\nPuerto HTTP: 5200",
                "Local Print Service");
            return 0;
        }

        if (state == ServiceState.Installed)
        {
            Ui.ShowInfo(
                "El servicio de impresión local está instalado pero detenido.\nSe iniciará ahora.",
                "Local Print Service");
            return ServiceInstaller.Elevate(exePath, "--start") ? 0 : 1;
        }

        if (Ui.Ask(
                "El servicio de impresión local no está instalado.\n\n¿Desea instalarlo e iniciarlo ahora?",
                "Local Print Service"))
        {
            if (ServiceInstaller.Elevate(exePath, "--install"))
            {
                return 0;
            }

            Ui.ShowError(
                "La instalación requiere permisos de administrador.\nAcepte la ventana de Control de cuentas de usuario (UAC) y vuelva a intentarlo.",
                "Local Print Service");
            return 1;
        }

        return 0;
    }
}