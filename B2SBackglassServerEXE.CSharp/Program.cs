using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace B2SBackglassServerEXE
{
    static class Program
    {
        public static string TableFileName { get; set; } = string.Empty;
        public static string GameName { get; set; } = string.Empty;
        public static string B2SName { get; set; } = string.Empty;
        public static bool PureEXE { get; set; } = false;

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            try
            {
                ParseCommandLineArguments(args);
                LoadRegistrySettings();
                
                if (string.IsNullOrEmpty(TableFileName))
                {
                    MessageBox.Show("Please do not start the EXE this way.", 
                        "B2S Backglass Server", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                    return;
                }
                
                Application.Run(new Forms.BackglassForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error: {ex.Message}\n\n{ex.StackTrace}", 
                    "B2S Backglass Server Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        private static void ParseCommandLineArguments(string[] args)
        {
            if (args.Length == 0)
                return;

            TableFileName = args[0];

            // Check if it's a direct .directb2s file
            if (TableFileName.EndsWith(".directb2s", StringComparison.OrdinalIgnoreCase))
            {
                TableFileName = Path.GetFileNameWithoutExtension(TableFileName);
                PureEXE = true;
                GameName = string.Empty;
                B2SName = string.Empty;
            }
            else
            {
                // Normal launch from COM server - will read from registry
                PureEXE = false;
            }

            // Check for TopMost flag
            if (args.Length > 1 && args[1] == "1")
            {
                // Form will be set to TopMost
            }
        }

        private static void LoadRegistrySettings()
        {
            if (PureEXE)
                return;

            try
            {
                using (var regkey = Registry.CurrentUser.OpenSubKey("Software\\B2S"))
                {
                    if (regkey != null)
                    {
                        GameName = regkey.GetValue("B2SGameName", string.Empty) as string ?? string.Empty;
                        B2SName = regkey.GetValue("B2SB2SName", string.Empty) as string ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't crash
                System.Diagnostics.Debug.WriteLine($"Registry read error: {ex.Message}");
            }

            // Fallback: if TableFileName is empty, use GameName
            if (string.IsNullOrEmpty(TableFileName))
            {
                TableFileName = GameName;
            }
        }
    }
}
