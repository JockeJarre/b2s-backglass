using System;
using System.Drawing;

namespace B2SBackglassServerEXE.Utilities
{
    public static class ImageScaler
    {
        public static Image ScaleImage(Image original, Size targetSize, bool maintainAspectRatio = false)
        {
            if (original == null)
                return null;

            if (original.Width == targetSize.Width && original.Height == targetSize.Height)
                return original;

            Size newSize = targetSize;

            if (maintainAspectRatio)
            {
                float ratioX = (float)targetSize.Width / original.Width;
                float ratioY = (float)targetSize.Height / original.Height;
                float ratio = Math.Min(ratioX, ratioY);

                newSize = new Size(
                    (int)(original.Width * ratio),
                    (int)(original.Height * ratio)
                );
            }

            var scaledImage = new Bitmap(newSize.Width, newSize.Height);
            using (var graphics = Graphics.FromImage(scaledImage))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                graphics.DrawImage(original, 0, 0, newSize.Width, newSize.Height);
            }

            return scaledImage;
        }

        public static SizeF GetScaleFactor(Size original, Size target)
        {
            if (original.Width == 0 || original.Height == 0 || target.Width == 0 || target.Height == 0)
                return new SizeF(1.0f, 1.0f);

            return new SizeF(
                (float)original.Width / target.Width,
                (float)original.Height / target.Height
            );
        }

        public static Point ScalePoint(Point point, SizeF scaleFactor)
        {
            return new Point(
                (int)(point.X * scaleFactor.Width),
                (int)(point.Y * scaleFactor.Height)
            );
        }

        public static Size ScaleSize(Size size, SizeF scaleFactor)
        {
            return new Size(
                (int)(size.Width * scaleFactor.Width),
                (int)(size.Height * scaleFactor.Height)
            );
        }

        public static Rectangle ScaleRectangle(Rectangle rect, SizeF scaleFactor)
        {
            return new Rectangle(
                ScalePoint(rect.Location, scaleFactor),
                ScaleSize(rect.Size, scaleFactor)
            );
        }
    }
}
