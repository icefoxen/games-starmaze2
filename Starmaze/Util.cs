using System;
using System.Diagnostics;

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
		// These Conditional() flags mark these methods as never getting called if we are not 
		// making a DEBUG build.  Monodevelop sets the DEBUG #define or not based
		// on project build mode.  Supposedly; in reality, it seems a little conservative
		// about rebuilding the project when you switch modes, and has to be forced
		// to rebuild everything.
		[Conditional ("DEBUG")]
		public static void Assert(bool assertion)
		{
			// TODO: Implement this, since on Windows an assertion failure pops up a box that
			// has an 'ignore' button, and assertions are for things that should never happen
			Console.WriteLine("Foo!");
		}

		[Conditional ("DEBUG")]
		public static void Warn(bool assertion)
		{
			// TODO: Implement this for cases where an assertion is too strong;
			// ie, recoverable errors that nonetheless should never happen.
		}

		[Conditional ("DEBUG")]
		public static void Log(string message)
		{
			// TODO: Implement this for messages (like OpenGL version) that are useful to have
			// but which the user shouldn't confront unless they look for it.
		}
	}
}

