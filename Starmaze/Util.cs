using System;

namespace Starmaze
{
	public static class Util
	{
		public const string WindowTitle = "Starmaze";
		public const int GlMajorVersion = 3;
		public const int GlMinorVersion = 3;
		public static bool IsPowerOf2(int i) {
			return (i != 0) && ((i & (i - 1)) == 0);
		}
	}
}

