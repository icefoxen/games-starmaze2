using System;
using OpenTK;
using OpenTK.Graphics;

namespace Starmaze
{
	/// <summary>
	/// random math stuff.
	/// The S is for Starmaze, to disambiguate from System.Math easily.
	/// </summary>
	public static class SMath
	{
		public const double TAU = Math.PI * 2;
		public const double PIOVER2 = Math.PI / 2;

		public static double CrossZ(Vector2d v0, Vector2d v1)
		{
			return v0.X * v1.Y - v0.Y * v1.X;
		}
		// BUGGO: Verify sign, direction, units (degrees/radians)
		// XXX: Always rotates around the origin.
		public static Vector2d Rotate(Vector2d vec, double amount)
		{
			var theta = Math.Atan2(vec.Y, vec.X);
			theta += amount;
			var cs = Math.Cos(theta);
			var sn = Math.Sin(theta);
			var x = vec.X * cs - vec.Y * sn;
			var y = vec.X * sn + vec.Y * cs;
			return new Vector2d(x, y);
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

		/// <summary>
		/// Limits the number to within the given bounds.
		/// </summary>
		/// <param name="number">Number.</param>
		/// <param name="low">Low.</param>
		/// <param name="high">High.</param>
		public static double Clamp(double number, double low, double high)
		{
			if (number < low) {
				return low;
			} else if (number > high) {
				return high;
			} else {
				return number;
			}
		}

		/// <summary>
		/// Rounds up to nearest power of 2.
		/// Algorithm:
		/// 2^(ceil(log2(x)))
		/// </summary>
		/// <returns>Number.</returns>
		/// <param name="number">Number.</param>
		public static double RoundUpToPowerOf2(double number)
		{
			return Math.Pow(2, Math.Ceiling(Math.Log(number, 2)));
		}
	}
}
