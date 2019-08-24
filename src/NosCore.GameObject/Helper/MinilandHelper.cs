using System;
using System.Collections.Generic;
using System.Linq;
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

        public Gift GetMinilandGift(short game, int? point)
        {
            List<Gift> gifts = new List<Gift>();
            Random rand = new Random();
            switch (game)
            {
                case 3117:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 3));
                            gifts.Add(new Gift(2100, 3));
                            gifts.Add(new Gift(2102, 3));
                            break;

                        case 1:
                            gifts.Add(new Gift(1031, 2));
                            gifts.Add(new Gift(1032, 2));
                            gifts.Add(new Gift(1033, 2));
                            gifts.Add(new Gift(1034, 2));
                            gifts.Add(new Gift(2205, 1));
                            break;

                        case 2:
                            gifts.Add(new Gift(2205, 1));
                            gifts.Add(new Gift(2189, 1));
                            gifts.Add(new Gift(2034, 2));
                            break;

                        case 3:
                            gifts.Add(new Gift(2205, 1));
                            gifts.Add(new Gift(2189, 1));
                            gifts.Add(new Gift(2034, 2));
                            gifts.Add(new Gift(2105, 1));
                            break;

                        case 4:
                            gifts.Add(new Gift(2205, 1));
                            gifts.Add(new Gift(2189, 1));
                            gifts.Add(new Gift(2034, 2));
                            gifts.Add(new Gift(2193, 1));
                            break;
                    }
                    break;

                case 3118:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 3));
                            gifts.Add(new Gift(2100, 3));
                            gifts.Add(new Gift(2102, 3));
                            break;

                        case 1:
                            gifts.Add(new Gift(2206, 1));
                            gifts.Add(new Gift(2032, 3));
                            break;

                        case 2:
                            gifts.Add(new Gift(2206, 1));
                            gifts.Add(new Gift(2106, 1));
                            gifts.Add(new Gift(2038, 2));
                            break;

                        case 3:
                            gifts.Add(new Gift(2206, 1));
                            gifts.Add(new Gift(2190, 1));
                            gifts.Add(new Gift(2039, 2));
                            gifts.Add(new Gift(2109, 1));
                            break;

                        case 4:
                            gifts.Add(new Gift(2206, 1));
                            gifts.Add(new Gift(2190, 1));
                            gifts.Add(new Gift(2040, 2));
                            gifts.Add(new Gift(2194, 1));
                            break;
                    }
                    break;

                case 3119:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 3));
                            gifts.Add(new Gift(2100, 3));
                            gifts.Add(new Gift(2102, 3));
                            break;

                        case 1:
                            gifts.Add(new Gift(2027, 15));
                            gifts.Add(new Gift(2207, 1));
                            break;

                        case 2:
                            gifts.Add(new Gift(2207, 1));
                            gifts.Add(new Gift(2046, 2));
                            gifts.Add(new Gift(2191, 1));
                            break;

                        case 3:
                            gifts.Add(new Gift(2207, 1));
                            gifts.Add(new Gift(2047, 2));
                            gifts.Add(new Gift(2191, 1));
                            gifts.Add(new Gift(2117, 1));
                            break;

                        case 4:
                            gifts.Add(new Gift(2207, 1));
                            gifts.Add(new Gift(2048, 2));
                            gifts.Add(new Gift(2191, 1));
                            gifts.Add(new Gift(2195, 1));
                            break;
                    }
                    break;

                case 3120:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 3));
                            gifts.Add(new Gift(2100, 3));
                            gifts.Add(new Gift(2102, 3));
                            break;

                        case 1:
                            gifts.Add(new Gift(2208, 1));
                            gifts.Add(new Gift(2017, 10));
                            break;

                        case 2:
                            gifts.Add(new Gift(2208, 1));
                            gifts.Add(new Gift(2192, 1));
                            gifts.Add(new Gift(2042, 2));
                            break;

                        case 3:
                            gifts.Add(new Gift(2208, 1));
                            gifts.Add(new Gift(2192, 1));
                            gifts.Add(new Gift(2043, 2));
                            gifts.Add(new Gift(2118, 1));
                            break;

                        case 4:
                            gifts.Add(new Gift(2208, 1));
                            gifts.Add(new Gift(2192, 1));
                            gifts.Add(new Gift(2044, 2));
                            gifts.Add(new Gift(2196, 1));
                            break;
                    }
                    break;

                case 3121:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 4));
                            gifts.Add(new Gift(2100, 4));
                            gifts.Add(new Gift(2102, 4));
                            gifts.Add(new Gift(1031, 3));
                            gifts.Add(new Gift(1032, 3));
                            gifts.Add(new Gift(1033, 3));
                            gifts.Add(new Gift(1034, 3));
                            break;

                        case 1:
                            gifts.Add(new Gift(2034, 3));
                            gifts.Add(new Gift(2205, 1));
                            gifts.Add(new Gift(2189, 1));
                            break;

                        case 2:
                            gifts.Add(new Gift(2035, 3));
                            gifts.Add(new Gift(2193, 1));
                            gifts.Add(new Gift(2275, 1));
                            break;

                        case 3:
                            gifts.Add(new Gift(2036, 3));
                            gifts.Add(new Gift(2193, 1));
                            gifts.Add(new Gift(1028, 1));
                            break;

                        case 4:
                            gifts.Add(new Gift(2037, 3));
                            gifts.Add(new Gift(2193, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1029, 1));
                            gifts.Add(new Gift(2197, 1));
                            break;
                    }
                    break;

                case 3122:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 4));
                            gifts.Add(new Gift(2100, 4));
                            gifts.Add(new Gift(2102, 4));
                            gifts.Add(new Gift(2032, 4));
                            break;

                        case 1:
                            gifts.Add(new Gift(2038, 3));
                            gifts.Add(new Gift(2206, 1));
                            gifts.Add(new Gift(2190, 1));
                            break;

                        case 2:
                            gifts.Add(new Gift(2039, 3));
                            gifts.Add(new Gift(2194, 1));
                            gifts.Add(new Gift(2105, 1));
                            break;

                        case 3:
                            gifts.Add(new Gift(2040, 3));
                            gifts.Add(new Gift(2194, 1));
                            gifts.Add(new Gift(1028, 1));
                            break;

                        case 4:
                            gifts.Add(new Gift(2041, 3));
                            gifts.Add(new Gift(2194, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1029, 1));
                            gifts.Add(new Gift(2198, 1));
                            break;
                    }
                    break;

                case 3123:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 4));
                            gifts.Add(new Gift(2100, 4));
                            gifts.Add(new Gift(2102, 4));
                            gifts.Add(new Gift(2047, 15));
                            break;

                        case 1:
                            gifts.Add(new Gift(2046, 3));
                            gifts.Add(new Gift(2205, 1));
                            gifts.Add(new Gift(2189, 1));
                            break;

                        case 2:
                            gifts.Add(new Gift(2047, 3));
                            gifts.Add(new Gift(2195, 1));
                            gifts.Add(new Gift(2117, 1));
                            break;

                        case 3:
                            gifts.Add(new Gift(2048, 3));
                            gifts.Add(new Gift(2195, 1));
                            gifts.Add(new Gift(1028, 1));
                            break;

                        case 4:
                            gifts.Add(new Gift(2049, 3));
                            gifts.Add(new Gift(2195, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1029, 1));
                            gifts.Add(new Gift(2199, 1));
                            break;
                    }
                    break;

                case 3124:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2099, 4));
                            gifts.Add(new Gift(2100, 4));
                            gifts.Add(new Gift(2102, 4));
                            gifts.Add(new Gift(2017, 10));
                            break;

                        case 1:
                            gifts.Add(new Gift(2042, 3));
                            gifts.Add(new Gift(2192, 1));
                            gifts.Add(new Gift(2189, 1));
                            break;

                        case 2:
                            gifts.Add(new Gift(2043, 3));
                            gifts.Add(new Gift(2196, 1));
                            gifts.Add(new Gift(2118, 1));
                            break;

                        case 3:
                            gifts.Add(new Gift(2044, 3));
                            gifts.Add(new Gift(2196, 1));
                            gifts.Add(new Gift(1028, 1));
                            break;

                        case 4:
                            gifts.Add(new Gift(2045, 3));
                            gifts.Add(new Gift(2196, 1));
                            gifts.Add(new Gift(1028, 1));
                            gifts.Add(new Gift(1029, 1));
                            gifts.Add(new Gift(2200, 1));
                            break;
                    }
                    break;

                case 3125:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2034, 4));
                            gifts.Add(new Gift(2189, 2));
                            gifts.Add(new Gift(2205, 2));
                            break;

                        case 1:
                            gifts.Add(new Gift(2035, 4));
                            gifts.Add(new Gift(2105, 2));
                            break;

                        case 2:
                            gifts.Add(new Gift(2036, 4));
                            gifts.Add(new Gift(2193, 2));
                            break;

                        case 3:
                            gifts.Add(new Gift(2037, 4));
                            gifts.Add(new Gift(2193, 2));
                            gifts.Add(new Gift(2201, 2));
                            gifts.Add(new Gift(2226, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            break;

                        case 4:
                            gifts.Add(new Gift(2213, 1));
                            gifts.Add(new Gift(2193, 2));
                            gifts.Add(new Gift(2034, 2));
                            gifts.Add(new Gift(2226, 2));
                            gifts.Add(new Gift(1030, 2));
                            break;
                    }
                    break;

                case 3126:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2038, 4));
                            gifts.Add(new Gift(2106, 2));
                            gifts.Add(new Gift(2206, 2));
                            break;

                        case 1:
                            gifts.Add(new Gift(2039, 4));
                            gifts.Add(new Gift(2109, 2));
                            break;

                        case 2:
                            gifts.Add(new Gift(2040, 4));
                            gifts.Add(new Gift(2194, 2));
                            break;

                        case 3:
                            gifts.Add(new Gift(2040, 4));
                            gifts.Add(new Gift(2194, 2));
                            gifts.Add(new Gift(2201, 2));
                            gifts.Add(new Gift(2231, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            break;

                        case 4:
                            gifts.Add(new Gift(2214, 1));
                            gifts.Add(new Gift(2194, 1));
                            gifts.Add(new Gift(2231, 2));
                            gifts.Add(new Gift(2202, 1));
                            gifts.Add(new Gift(1030, 2));
                            break;
                    }
                    break;

                case 3127:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2046, 4));
                            gifts.Add(new Gift(2207, 2));
                            break;

                        case 1:
                            gifts.Add(new Gift(2047, 4));
                            gifts.Add(new Gift(2117, 2));
                            break;

                        case 2:
                            gifts.Add(new Gift(2048, 4));
                            gifts.Add(new Gift(2195, 2));
                            break;

                        case 3:
                            gifts.Add(new Gift(2049, 4));
                            gifts.Add(new Gift(2195, 2));
                            gifts.Add(new Gift(2199, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            break;

                        case 4:
                            gifts.Add(new Gift(2216, 1));
                            gifts.Add(new Gift(2195, 2));
                            gifts.Add(new Gift(2203, 1));
                            gifts.Add(new Gift(1030, 2));
                            break;
                    }
                    break;

                case 3128:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2042, 4));
                            gifts.Add(new Gift(2192, 2));
                            gifts.Add(new Gift(2208, 2));
                            break;

                        case 1:
                            gifts.Add(new Gift(2043, 4));
                            gifts.Add(new Gift(2118, 2));
                            break;

                        case 2:
                            gifts.Add(new Gift(2044, 4));
                            gifts.Add(new Gift(2196, 2));
                            break;

                        case 3:
                            gifts.Add(new Gift(2045, 4));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2200, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            break;

                        case 4:
                            gifts.Add(new Gift(2215, 1));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2204, 1));
                            gifts.Add(new Gift(1030, 2));
                            break;
                    }
                    break;

                // GM mini-game
                case 3130:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2042, 4));
                            gifts.Add(new Gift(2192, 2));
                            gifts.Add(new Gift(2208, 2));
                            break;

                        case 1:
                            gifts.Add(new Gift(2043, 4));
                            gifts.Add(new Gift(2118, 2));
                            break;

                        case 2:
                            gifts.Add(new Gift(2044, 4));
                            gifts.Add(new Gift(2196, 2));
                            break;

                        case 3:
                            gifts.Add(new Gift(2045, 4));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2200, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            break;

                        case 4:
                            gifts.Add(new Gift(2215, 1));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2204, 1));
                            gifts.Add(new Gift(1030, 2));
                            break;
                    }
                    break;

                case 3131:
                    switch (point)
                    {
                        case 0:
                            gifts.Add(new Gift(2042, 4));
                            gifts.Add(new Gift(2192, 2));
                            gifts.Add(new Gift(2208, 2));
                            break;

                        case 1:
                            gifts.Add(new Gift(2043, 4));
                            gifts.Add(new Gift(2118, 2));
                            break;

                        case 2:
                            gifts.Add(new Gift(2044, 4));
                            gifts.Add(new Gift(2196, 2));
                            break;

                        case 3:
                            gifts.Add(new Gift(2045, 4));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2200, 2));
                            gifts.Add(new Gift(1028, 2));
                            gifts.Add(new Gift(1029, 2));
                            break;

                        case 4:
                            gifts.Add(new Gift(2215, 1));
                            gifts.Add(new Gift(2196, 2));
                            gifts.Add(new Gift(2204, 1));
                            gifts.Add(new Gift(1030, 2));
                            break;
                    }
                    break;
            }
            return gifts.OrderBy(s => rand.Next()).FirstOrDefault();
        }
    }
}