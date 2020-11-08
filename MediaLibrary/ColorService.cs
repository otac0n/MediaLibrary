namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    public static class ColorService
    {
        private static Dictionary<string, Color?> cache = new Dictionary<string, Color?>();

        public static Color ContrastColor(Color? color)
        {
            if (color == null)
            {
                return Color.Black;
            }

            double HumanBrightness(Color c)
            {
                return
                    0.25 * Math.Pow(c.R / 255.0, 1.8 * 2) +
                    0.54 * Math.Pow(c.G / 255.0, 1.8 * 2) +
                    0.21 * Math.Pow(c.B / 255.0, 1.8 * 2);
            }

            double Contrast(double l1, double l2)
            {
                return (Math.Max(l1, l2) + 0.25) / (Math.Min(l1, l2) + 0.25);
            }

            var brightness = HumanBrightness(color.Value);
            var whiteR = Contrast(brightness, HumanBrightness(Color.White));
            var blackR = Contrast(brightness, HumanBrightness(Color.Black));
            return whiteR > blackR ? Color.White : Color.Black;
        }

        public static Color? ParseColor(string color)
        {
            if (string.IsNullOrEmpty(color))
            {
                return null;
            }

            lock (cache)
            {
                if (cache.TryGetValue(color, out var cached))
                {
                    return cached;
                }
            }

            Color? value;
            try
            {
                value = new ColorParser().Parse(color.Trim());
            }
            catch (FormatException)
            {
                value = null;
            }

            lock (cache)
            {
                return cache[color] = value;
            }
        }
    }
}
