using System;
using System.Drawing;
using OpenTK;

namespace Starmaze.Engine
{
	/// <summary>
	/// random math stuff.
	/// The S is for Starmaze, to disambiguate from System.Math easily.
	/// </summary>
	public static class SMath
	{
		public const double TAU = Math.PI * 2;

		public static double CrossZ(Vector2d v0, Vector2d v1)
		{
			return v0.X * v1.Y - v0.Y * v1.X;
		}
		// Handy interpolation functions for types that don't include
		// their own.
		public static float Lerp(float a, float b, float alpha)
		{
			return a + (b - a) * alpha;
		}

		public static double Lerp(double a, double b, double alpha)
		{
			return a + (b - a) * alpha;
		}

		public static Color Lerp(Color c1, Color c2, double alpha)
		{
			var r = (int)Math.Round(c1.R + (c2.R - c1.R) * alpha);
			var b = (int)Math.Round(c1.B + (c2.B - c1.B) * alpha);
			var g = (int)Math.Round(c1.G + (c2.G - c1.G) * alpha);
			var a = (int)Math.Round(c1.A + (c2.A - c1.A) * alpha);
			return Color.FromArgb(a, r, g, b);
		}
	}
}
