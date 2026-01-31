using System;
using System.Runtime.InteropServices;

namespace B2SBackglassServerEXE.Utilities
{
    /// <summary>
    /// Win32 API P/Invoke declarations
    /// </summary>
    public static class Win32Api
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        // Window messages
        public const uint WM_SYSCOMMAND = 0x0112;
        public const uint WM_CLOSE = 0x0010;
        public const int SC_CLOSE = 0xF060;

        // DPI awareness
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("SHCore.dll", SetLastError = true)]
        public static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

        public enum PROCESS_DPI_AWARENESS
        {
            Process_DPI_Unaware = 0,
            Process_System_DPI_Aware = 1,
            Process_Per_Monitor_DPI_Aware = 2
        }
    }
}
