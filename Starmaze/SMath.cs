using System;
using OpenTK;
using OpenTK.Graphics;

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

		public static Color4 Lerp(Color4 c1, Color4 c2, double alpha)
		{
			var r = (float)Math.Round(c1.R + (c2.R - c1.R) * alpha);
			var b = (float)Math.Round(c1.B + (c2.B - c1.B) * alpha);
			var g = (float)Math.Round(c1.G + (c2.G - c1.G) * alpha);
			var a = (float)Math.Round(c1.A + (c2.A - c1.A) * alpha);
			return new Color4(r, g, b, a);
		}
	}
}
