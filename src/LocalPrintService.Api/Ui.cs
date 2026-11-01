using System.Runtime.InteropServices;

namespace LocalPrintService.Api;

internal static class Ui
{
    private const uint MbOk = 0x0000;
    private const uint MbYesNo = 0x0004;
    private const uint MbIconError = 0x0010;
    private const uint MbIconQuestion = 0x0020;
    private const uint MbIconInformation = 0x0040;

    private const int IdYes = 6;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    public static bool Ask(string message, string title)
    {
        if (!Environment.UserInteractive)
        {
            return false;
        }

        return MessageBoxW(IntPtr.Zero, message, title, MbYesNo | MbIconQuestion) == IdYes;
    }

    public static void ShowInfo(string message, string title)
    {
        if (Environment.UserInteractive)
        {
            MessageBoxW(IntPtr.Zero, message, title, MbOk | MbIconInformation);
        }
    }

    public static void ShowError(string message, string title)
    {
        if (Environment.UserInteractive)
        {
            MessageBoxW(IntPtr.Zero, message, title, MbOk | MbIconError);
        }
    }
}