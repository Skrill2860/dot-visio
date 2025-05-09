using System;
using System.Drawing;

namespace Common;

public static class ColorConverter
{
    public static string ColorToHexRgbString(Color color)
    {
        return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
    }

    public static Color ColorFromHsv(Hsv hsv)
    {
        return ColorFromHsv(hsv.H, hsv.S, hsv.V, hsv.A);
    }

    private static Color ColorFromHsv(double hue, double saturation, double value, double alpha = 1)
    {
        hue = hue * 360;
        var a = (int)(255 * alpha);
        var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        var f = hue / 60 - Math.Floor(hue / 60);

        value = value * 255;
        var v = Convert.ToInt32(value);
        var p = Convert.ToInt32(value * (1 - saturation));
        var q = Convert.ToInt32(value * (1 - f * saturation));
        var t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        if (hi == 0)
        {
            return Color.FromArgb(a, v, t, p);
        }

        if (hi == 1)
        {
            return Color.FromArgb(a, q, v, p);
        }

        if (hi == 2)
        {
            return Color.FromArgb(a, p, v, t);
        }

        if (hi == 3)
        {
            return Color.FromArgb(a, p, q, v);
        }

        if (hi == 4)
        {
            return Color.FromArgb(a, t, p, v);
        }

        return Color.FromArgb(a, v, p, q);
    }

    public class Hsv
    {
        private double _a;
        private double _h;
        private double _v;
        private double _s;

        public Hsv()
        {
            _h = 0d;
            _s = 0d;
            _v = 0d;
            _a = 1;
        }

        public double H
        {
            get => _h;

            set => _h = value > 1d ? 1d : value < 0d ? 0d : value;
        }

        public double S
        {
            get => _s;
            set => _s = value > 1d ? 1d : value < 0d ? 0d : value;
        }

        public double V
        {
            get => _v;
            set => _v = value > 1d ? 1d : value < 0d ? 0d : value;
        }

        public double A
        {
            get => _a;
            set => _a = value > 1d ? 1d : value < 0d ? 0d : value;
        }
    }
}