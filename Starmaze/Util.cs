using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Starmaze
{
	/// <summary>
	/// A class containing various handy static values and functions.
	/// </summary>
	public static class Util
	{
		public const string WindowTitle = "Starmaze";
		public const int GlMajorVersion = 3;
		public const int GlMinorVersion = 3;
		public const int LogicalScreenWidth = 160;
		public const string SettingsFileName = "settings.cfg";

		public static readonly Version StarmazeVersion = new Version(0, 1);

		public static bool IsPowerOf2(int i)
		{
			return (i != 0) && ((i & (i - 1)) == 0);
		}

		private static long _serial = 0;

		/// <summary>
		/// Returns a serial number that is never the same twice (to within the accuracy of a long at least).
		/// Generally used for assigning arbitrary-but-consistent ordering to objects.
		/// </summary>
		/// <returns>Serial number</returns>
		public static long GetSerial()
		{
			_serial += 1;
			return _serial;
		}

		public static IEnumerable<Type> GetSubclassesOf(Type baseType)
		{
			var assembly = baseType.Assembly;
			// Monodevelop stupidly doesn't know about Linq.
			var subclasses = assembly.GetTypes().Where(t => t.IsSubclassOf(baseType));
			return subclasses;
		}

		public static IEnumerable<Type> GetImplementorsOf(Type baseType)
		{
			var assembly = baseType.Assembly;
			// Monodevelop stupidly doesn't know about Linq.

			var subclasses = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(baseType));
			//Log.Message("Subclasses: {0}", subclasses);
			return subclasses;
		}
		// XXX: Does C# already have a generator like this somewhere?
		// step < 0 doesn't work.
		public static IEnumerable<uint> UnsignedRange(uint start, uint to, int step = 1)
		{
			//Console.WriteLine("Got {0}, {1}, {2}", start, to, step);
			for (uint i = start; i < to; i = (uint)(i + step)) {
				yield return i;
			}
		}

		// BUGGO: There's really no way to do this in C#?
		public static T[] InitArray<T>(int length, T initItem)
		{
			var arr = new T[length];
			for (int i = 0; i < arr.Length; i++) {
				arr[i] = initItem;
			}
			return arr;
		}
	}

}

