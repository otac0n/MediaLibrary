namespace MediaLibrary.Services
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    public static class ColorService
    {
        public const double Gamma = 1.8;

        private static Dictionary<string, Color?> cache = new Dictionary<string, Color?>();

        public static Color Blend(double amount, Color a, Color b)
        {
            return Blend((amount, a), (1 - amount, b));
        }

        public static Color Blend(IEnumerable<Color> colors)
        {
            return Blend(colors.Select(c => (1D, c)));
        }

        public static Color Blend(params Color[] colors)
        {
            return Blend(colors.Select(c => (1D, c)));
        }

        public static Color Blend(params (double, Color)[] colors)
        {
            return Blend(colors.AsEnumerable());
        }

        public static Color Blend(IEnumerable<(double weight, Color value)> colors)
        {
            var r = 0.0;
            var g = 0.0;
            var b = 0.0;
            var weight = 0.0;

            foreach (var color in colors)
            {
                r += Math.Pow(color.value.R, Gamma) * color.weight;
                g += Math.Pow(color.value.G, Gamma) * color.weight;
                b += Math.Pow(color.value.B, Gamma) * color.weight;
                weight += color.weight;
            }

            return Color.FromArgb(
                (int)Math.Round(Math.Pow(r / weight, 1 / Gamma)),
                (int)Math.Round(Math.Pow(g / weight, 1 / Gamma)),
                (int)Math.Round(Math.Pow(b / weight, 1 / Gamma)));
        }

        public static double Contrast(Color a, Color b) =>
            ContrastRatio(
                ContrastBrightness(a),
                ContrastBrightness(b));

        public static double ContrastBrightness(Color color) =>
            ContrastBrightness(color, Gamma);

        public static double ContrastBrightness(Color color, double gamma) =>
            0.25 * Math.Pow(color.R / 255.0, gamma * 2) +
            0.54 * Math.Pow(color.G / 255.0, gamma * 2) +
            0.21 * Math.Pow(color.B / 255.0, gamma * 2);

        public static Color ContrastColor(Color? color) =>
            color is Color c ? ContrastColor(c) : Color.Black;

        public static Color ContrastColor(Color color)
        {
            var brightness = ContrastBrightness(color);
            var whiteContrast = ContrastRatio(brightness, ContrastBrightness(Color.White));
            var blackContrast = ContrastRatio(brightness, ContrastBrightness(Color.Black));
            return whiteContrast > blackContrast ? Color.White : Color.Black;
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

        private static double ContrastRatio(double l1, double l2)
        {
            return (Math.Max(l1, l2) + 0.25) / (Math.Min(l1, l2) + 0.25);
        }
    }
}
