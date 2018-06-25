using System;

namespace NosCore.PathFinder
{
	public static class Heuristic
	{
		public static readonly double Sqrt2 = Math.Sqrt(2);

		public static double Octile(int iDx, int iDy)
		{
			var min = Math.Min(iDx, iDy);
			var max = Math.Max(iDx, iDy);
			return (min * Sqrt2) + max - min;
		}
	}
}