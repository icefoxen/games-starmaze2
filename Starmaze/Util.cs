using System;

namespace Starmaze
{
	public static class Util
	{
		public static bool IsPowerOf2(int i) {
			return (i != 0) && ((i & (i - 1)) == 0);
		}
	}
}

