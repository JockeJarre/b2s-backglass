using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace B2SBackglassServerEXE.Utilities
{
    public class ScreenManager
    {
        public static Screen GetBackglassScreen()
        {
            // For now, just return primary screen
            // TODO: Read from settings when monitor selection is implemented
            return Screen.PrimaryScreen;
        }

        public static Point GetBackglassLocation(Size backglassSize)
        {
            var screen = GetBackglassScreen();

            // Center on screen
            int x = screen.Bounds.X + (screen.Bounds.Width - backglassSize.Width) / 2;
            int y = screen.Bounds.Y + (screen.Bounds.Height - backglassSize.Height) / 2;

            return new Point(x, y);
        }

        public static Point GetDMDLocation(Point backglassLocation, Size backglassSize, Point dmdOffset, Size dmdSize)
        {
            if (dmdOffset == Point.Empty)
            {
                return new Point(
                    backglassLocation.X + backglassSize.Width + 10,
                    backglassLocation.Y
                );
            }

            return new Point(
                backglassLocation.X + dmdOffset.X,
                backglassLocation.Y + dmdOffset.Y
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
