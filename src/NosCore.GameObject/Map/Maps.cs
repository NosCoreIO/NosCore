using NosCore.PathFinder;
using System;

namespace NosCore.GameObject.Map
{
    public static class Maps
    {
        #region Method
        public static int GetDistance(short firtX, short firstY, short secondX, short secondY)
        {
            return (int)Heuristic.Octile(Math.Abs(firtX - secondX), Math.Abs(firstY - secondY));
        }
        #endregion
    }
}
