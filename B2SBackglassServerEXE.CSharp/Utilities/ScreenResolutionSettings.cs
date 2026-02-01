using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace B2SBackglassServerEXE.Utilities
{
    public class ScreenResolutionSettings
    {
        public Size PlayfieldSize { get; set; }
        public Size BackglassSize { get; set; }
        public int BackglassScreenNumber { get; set; }
        public Point BackglassLocation { get; set; }
        public Size DMDSize { get; set; }
        public Point DMDLocation { get; set; }
        public bool DMDFlipY { get; set; }
        public Point BackgroundLocation { get; set; }
        public Size BackgroundSize { get; set; }
        public string BackgroundPath { get; set; } = "";

        public static ScreenResolutionSettings? Load()
        {
            string currentDir = Directory.GetCurrentDirectory();
            string[] files = Directory.GetFiles(currentDir, "*ScreenRes*.txt");

            if (files.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("No ScreenRes file found, using defaults");
                return GetDefaults();
            }

            try
            {
                string settingsFile = files[0];
                System.Diagnostics.Debug.WriteLine($"Loading screen settings from: {settingsFile}");

                var lines = File.ReadAllLines(settingsFile)
                    .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"))
                    .ToArray();

                if (lines.Length < 10)
                {
                    System.Diagnostics.Debug.WriteLine("Not enough lines in ScreenRes file");
                    return GetDefaults();
                }

                var settings = new ScreenResolutionSettings();

                // Parse each line
                settings.PlayfieldSize = new Size(
                    ParseInt(lines[0], 1920),
                    ParseInt(lines[1], 1080)
                );

                settings.BackglassSize = new Size(
                    ParseInt(lines[2], 1920),
                    ParseInt(lines[3], 1080)
                );

                settings.BackglassScreenNumber = ParseInt(lines[4], 1);

                settings.BackglassLocation = new Point(
                    ParseInt(lines[5], 0),
                    ParseInt(lines[6], 0)
                );

                settings.DMDSize = new Size(
                    ParseInt(lines[7], 128),
                    ParseInt(lines[8], 32)
                );

                settings.DMDLocation = new Point(
                    ParseInt(lines[9], 0),
                    ParseInt(lines[10], 0)
                );

                if (lines.Length > 11)
                    settings.DMDFlipY = ParseInt(lines[11], 0) == 1;

                if (lines.Length > 13)
                {
                    settings.BackgroundLocation = new Point(
                        ParseInt(lines[12], 0),
                        ParseInt(lines[13], 0)
                    );
                }

                if (lines.Length > 15)
                {
                    settings.BackgroundSize = new Size(
                        ParseInt(lines[14], 0),
                        ParseInt(lines[15], 0)
                    );
                }

                if (lines.Length > 16)
                {
                    settings.BackgroundPath = lines[16].Trim();
                }

                System.Diagnostics.Debug.WriteLine($"Loaded settings: Backglass={settings.BackglassSize}, Screen={settings.BackglassScreenNumber}");

                return settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading screen settings: {ex.Message}");
                return GetDefaults();
            }
        }

        private static ScreenResolutionSettings GetDefaults()
        {
            // Use second screen if available, otherwise primary
            var screens = Screen.AllScreens.OrderBy(s => s.Bounds.X).ToArray();
            var targetScreen = screens.Length > 1 ? screens[1] : screens[0];

            return new ScreenResolutionSettings
            {
                PlayfieldSize = new Size(1920, 1080),
                BackglassSize = new Size(targetScreen.Bounds.Width, targetScreen.Bounds.Height),
                BackglassScreenNumber = 1,
                BackglassLocation = Point.Empty,
                DMDSize = new Size(128, 32),
                DMDLocation = Point.Empty,
                DMDFlipY = false,
                BackgroundLocation = Point.Empty,
                BackgroundSize = Size.Empty,
                BackgroundPath = ""
            };
        }

        private static int ParseInt(string value, int defaultValue)
        {
            if (int.TryParse(value.Trim(), out int result))
                return result;
            return defaultValue;
        }
    }
}
