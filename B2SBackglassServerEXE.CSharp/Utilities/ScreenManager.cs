using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace B2SBackglassServerEXE.Utilities
{
    public class ScreenManager
    {
        private static ScreenResolutionSettings? _screenSettings;

        static ScreenManager()
        {
            _screenSettings = ScreenResolutionSettings.Load();
        }

        public static Screen GetBackglassScreen()
        {
            if (_screenSettings == null)
                return Screen.PrimaryScreen;

            var screens = Screen.AllScreens.OrderBy(s => s.Bounds.X).ToArray();
            
            // Screen number: 0 = primary, 1+ = ordered by X position
            int screenIndex = _screenSettings.BackglassScreenNumber;
            
            if (screenIndex >= 0 && screenIndex < screens.Length)
                return screens[screenIndex];

            // Default to second screen if available, else primary
            return screens.Length > 1 ? screens[1] : screens[0];
        }

        public static Point GetBackglassLocation()
        {
            var screen = GetBackglassScreen();
            
            if (_screenSettings != null && _screenSettings.BackglassLocation != Point.Empty)
            {
                return new Point(
                    screen.Bounds.X + _screenSettings.BackglassLocation.X,
                    screen.Bounds.Y + _screenSettings.BackglassLocation.Y
                );
            }

            // Default to screen origin
            return screen.Bounds.Location;
        }

        public static Size GetBackglassSize()
        {
            if (_screenSettings != null && _screenSettings.BackglassSize != Size.Empty)
            {
                return _screenSettings.BackglassSize;
            }

            // Default to full screen
            var screen = GetBackglassScreen();
            return screen.Bounds.Size;
        }

        public static Point GetDMDLocation(Point backglassLocation, Size backglassSize, Point dmdOffsetFromFile, Size dmdSize)
        {
            if (_screenSettings != null && _screenSettings.DMDLocation != Point.Empty)
            {
                return new Point(
                    backglassLocation.X + _screenSettings.DMDLocation.X,
                    backglassLocation.Y + _screenSettings.DMDLocation.Y
                );
            }

            // Use offset from .directb2s file if specified
            if (dmdOffsetFromFile != Point.Empty)
            {
                return new Point(
                    backglassLocation.X + dmdOffsetFromFile.X,
                    backglassLocation.Y + dmdOffsetFromFile.Y
                );
            }

            // Default: to the right of backglass
            return new Point(
                backglassLocation.X + backglassSize.Width + 10,
                backglassLocation.Y
            );
        }

        public static SizeF GetDpiScaleFactor()
        {
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiX = g.DpiX;
                float dpiY = g.DpiY;

                return new SizeF(dpiX / 96f, dpiY / 96f);
            }
        }

        public static void EnsureVisibleOnScreen(Form form)
        {
            bool isVisible = false;
            
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(
                    new Rectangle(form.Location, form.Size)))
                {
                    isVisible = true;
                    break;
                }
            }

            if (!isVisible)
            {
                var primaryScreen = Screen.PrimaryScreen;
                form.Location = new Point(
                    primaryScreen.WorkingArea.X + 50,
                    primaryScreen.WorkingArea.Y + 50
                );
            }
        }
    }
}
