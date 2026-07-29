using System.Runtime.InteropServices;
using System.Text;

namespace LocalPrintService.Infrastructure.Printing;

internal static class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct DOC_INFO_1
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDocName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDatatype;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Ansi, SetLastError = true)]
    internal static extern bool OpenPrinter(string pPrinterName, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOC_INFO_1 pDocInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    internal static extern bool WritePrinter(IntPtr hPrinter, byte[] pBuf, int cbBuf, out int pcWritten);

    public static (bool Success, string Error) SendBytesToPrinter(string printerName, byte[] bytes, string docName)
    {
        var text = Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n").Replace("\n", "\r\n");
        var cleanBytes = Encoding.UTF8.GetBytes(text);
        IntPtr hPrinter = IntPtr.Zero;
        int written = 0;

        try
        {
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                return (false, $"OpenPrinter failed for '{printerName}'. Win32 error: {error}");
            }

            var docInfo = new DOC_INFO_1
            {
                pDocName = docName,
                pOutputFile = null,
                pDatatype = "RAW"
            };

            if (!StartDocPrinter(hPrinter, 1, ref docInfo))
            {
                int error = Marshal.GetLastWin32Error();
                ClosePrinter(hPrinter);
                return (false, $"StartDocPrinter failed. Win32 error: {error}");
            }

            if (!StartPagePrinter(hPrinter))
            {
                int error = Marshal.GetLastWin32Error();
                EndDocPrinter(hPrinter);
                ClosePrinter(hPrinter);
                return (false, $"StartPagePrinter failed. Win32 error: {error}");
            }

            if (!WritePrinter(hPrinter, cleanBytes, cleanBytes.Length, out written))
            {
                int error = Marshal.GetLastWin32Error();
                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
                ClosePrinter(hPrinter);
                return (false, $"WritePrinter failed. Win32 error: {error}");
            }

            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);

            return (true, $"Wrote {written} bytes");
        }
        catch (Exception ex)
        {
            return (false, $"Exception: {ex.Message}");
        }
        finally
        {
            if (hPrinter != IntPtr.Zero)
                ClosePrinter(hPrinter);
        }
    }
}
