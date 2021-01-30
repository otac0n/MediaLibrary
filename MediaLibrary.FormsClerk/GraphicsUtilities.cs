// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;

    /// <summary>
    /// Provides common graphics utilities.
    /// </summary>
    public static class GraphicsUtilities
    {
        /// <summary>
        /// The numer of points per inch.
        /// </summary>
        /// <remarks>
        /// See <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/ff684173(v=vs.85).aspx"/> for more info.
        /// </remarks>
        public const int PointsPerInch = 72;

        /// <summary>
        /// Adds a string to the text path using the specified font and graphics.
        /// </summary>
        /// <param name="textPath">The text path to update.</param>
        /// <param name="text">The text to add to the path.</param>
        /// <param name="font">The font to use.</param>
        /// <param name="graphics">The graphics to use to measure the font.</param>
        /// <param name="rectangle">The latout rectangle.</param>
        /// <param name="stringFormat">The string format to use.</param>
        public static void AddString(this GraphicsPath textPath, string text, Font font, Graphics graphics, RectangleF rectangle, StringFormat stringFormat) =>
            textPath.AddString(text, font.FontFamily, (int)font.Style, graphics.DpiY * font.SizeInPoints / PointsPerInch, rectangle, stringFormat);

        /// <summary>
        /// Draws an impage with semitransparency applied.
        /// </summary>
        /// <param name="graphics">The graphics to draw the image to.</param>
        /// <param name="image">The image to draw.</param>
        /// <param name="rectangle">The layout rectangle.</param>
        /// <param name="alpha">The alpha value to use. This value will be multiplied with any existing alpha values from the source image.</param>
        public static void DrawImageTransparent(this Graphics graphics, Image image, RectangleF rectangle, float alpha)
        {
            var attr = new ImageAttributes();
            attr.SetColorMatrix(new ColorMatrix(new[]
            {
                new float[] { 1, 0, 0, 0, 0 },
                new float[] { 0, 1, 0, 0, 0 },
                new float[] { 0, 0, 1, 0, 0 },
                new float[] { 0, 0, 0, alpha, 0 },
                new float[] { 0, 0, 0, 0, 1 },
            }));
            graphics.DrawImage(image, new[] { rectangle.Location, new PointF(rectangle.Right, rectangle.Top), new PointF(rectangle.Left, rectangle.Bottom) }, new RectangleF(Point.Empty, image.Size), GraphicsUnit.Pixel, attr);
        }

        /// <summary>
        /// Executes an action with the specified graphics configured for high quaility rendering.
        /// </summary>
        /// <param name="graphics">The graphics to configure.</param>
        /// <param name="action">The action to execute.</param>
        public static void HighQuality(this Graphics graphics, Action action)
        {
            var originalInterpolation = graphics.InterpolationMode;
            var originalSmoothingMode = graphics.SmoothingMode;
            var originalTextRenderingHint = graphics.TextRenderingHint;
            var originalCompositingQuality = graphics.CompositingQuality;
            try
            {
                graphics.InterpolationMode = InterpolationMode.High;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                action();
            }
            finally
            {
                graphics.InterpolationMode = originalInterpolation;
                graphics.SmoothingMode = originalSmoothingMode;
                graphics.TextRenderingHint = originalTextRenderingHint;
                graphics.CompositingQuality = originalCompositingQuality;
            }
        }

        /// <summary>
        /// Draws a string with an outline.
        /// </summary>
        /// <param name="graphics">The graphics object to draw the string to.</param>
        /// <param name="text">The text to render.</param>
        /// <param name="font">The font to use.</param>
        /// <param name="brush">The brush used to fill the text.</param>
        /// <param name="outlinePen">The pen used to outline the text.</param>
        /// <param name="rectangle">The layout rectangle.</param>
        /// <param name="stringFormat">The string format to use.</param>
        public static void OutlineString(this Graphics graphics, string text, Font font, Brush brush, Pen outlinePen, RectangleF rectangle, StringFormat stringFormat)
        {
            using (var textPath = new GraphicsPath())
            {
                textPath.AddString(text, font, graphics, rectangle, stringFormat);
                graphics.DrawPath(outlinePen, textPath);
                graphics.FillPath(brush, textPath);
            }
        }
    }
}
