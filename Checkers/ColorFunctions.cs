using System;
using System.Drawing;

namespace Checkers
{
  struct ColorFunctions
  {
    // allows for the adjustment of the brightness of a given color by some factor m
    public static Color AdjustBrightness(Color color, double m)
    {
      int r = (int)Math.Max(0, Math.Min(255, Math.Round((double)color.R * m)));
      int g = (int)Math.Max(0, Math.Min(255, Math.Round((double)color.G * m)));
      int b = (int)Math.Max(0, Math.Min(255, Math.Round((double)color.B * m)));

      return Color.FromArgb(r, g, b);
    }
  }
}