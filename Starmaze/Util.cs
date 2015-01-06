using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Starmaze
{
	public static class Util
	{
		public const string WindowTitle = "Starmaze";
		public const int GlMajorVersion = 3;
		public const int GlMinorVersion = 3;

		public static bool IsPowerOf2(int i)
		{
			return (i != 0) && ((i & (i - 1)) == 0);
		}

		private static long _serial = 0;

		/// <summary>
		/// Returns a serial number that is never the same twice (to within the accuracy of a long at least).
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
	}

	public static class Log
	{
		// These Conditional() flags mark these methods as never getting called if we are not
		// making a DEBUG build.  Monodevelop sets the DEBUG #define or not based
		// on project build mode.  Supposedly; in reality, it seems a little conservative
		// about rebuilding the project when you switch modes, and has to be forced
		// to rebuild.  Stupid.
		[Conditional("DEBUG")]
		public static void Assert(bool assertion)
		{
			// TODO: Implement this better, since on Windows an assertion failure pops up a box that
			// has an 'ignore' button, and assertions are for things that should never happen
			Debug.Assert(assertion);
		}

		[Conditional("DEBUG")]
		public static void Warn(bool assertion, string message = null)
		{
			// TODO: Implement this for cases where an assertion is too strong;
			// ie, recoverable errors that nonetheless should never happen.
			if (message == null) {
				Debug.Assert(assertion);
			} else {
				Debug.Assert(assertion, message);
			}
		}

		[Conditional("DEBUG")]
		public static void Message(string message)
		{
			// TODO: Implement this for messages (like OpenGL version) that are useful to have
			// but which the user shouldn't confront unless they look for it.
			Console.WriteLine(message);
		}
	}
}

