using System;

namespace NosCore.PathFinder
{
    public class Heuristic
    {
        #region Members

        public static readonly double SQRT_2 = Math.Sqrt(2);

        #endregion

        #region Methods

        public static double Chebyshev(int iDx, int iDy) => Math.Max(iDx, iDy);

        public static double Euclidean(int iDx, int iDy)
        {
            float tFdx = iDx;
            float tFdy = iDy;
            return Math.Sqrt((tFdx * tFdx) + (tFdy * tFdy));
        }

        public static double Manhattan(int iDx, int iDy) => iDx + iDy;

        public static double Octile(int iDx, int iDy)
        {
            int min = Math.Min(iDx, iDy);
            int max = Math.Max(iDx, iDy);
            return (min * SQRT_2) + max - min;
        }

        #endregion
    }
}
