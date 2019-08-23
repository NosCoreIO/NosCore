using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject.Helper
{
    public sealed class MinilandHelper
    {
        private static MinilandHelper _instance;

        public int[][] MinilandMaxPoint;

        private MinilandHelper()
        {
            LoadMinilandMaxPoint();
        }

        private void LoadMinilandMaxPoint()
        {
            MinilandMaxPoint = new int[6][];
            MinilandMaxPoint[0] = new[] { 999, 4999, 7999, 11999, 15999, 1000000 };
            MinilandMaxPoint[1] = new[] { 999, 4999, 9999, 13999, 17999, 1000000 };
            MinilandMaxPoint[2] = new[] { 999, 3999, 7999, 14999, 24999, 1000000 };
            MinilandMaxPoint[3] = new[] { 999, 3999, 7999, 11999, 19999, 1000000 };
            MinilandMaxPoint[4] = new[] { 999, 4999, 7999, 11999, 15999, 1000000 };
            MinilandMaxPoint[5] = new[] { 999, 4999, 7999, 11999, 15999, 1000000 };
        }

        public static MinilandHelper Instance => _instance ?? (_instance = new MinilandHelper());
    }
}