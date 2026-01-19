//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.MailService;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.MinilandService
{
    public sealed class MinilandHelper
    {
        private static MinilandHelper? _instance;
        private readonly Dictionary<short, Gift[][]> _gifts = new()
        {
                {
                    3117,
                    new[]
                    {
                        new[] {new Gift(2099, 3), new Gift(2100, 3), new Gift(2102, 3)},
                        new[]
                        {
                            new Gift(1031, 2), new Gift(1032, 2), new Gift(1033, 2), new Gift(1034, 2),
                            new Gift(2205, 1)
                        },
                        new[] {new Gift(2205, 1), new Gift(2189, 1), new Gift(2034, 2)},
                        new[] {new Gift(2205, 1), new Gift(2189, 1), new Gift(2034, 2), new Gift(2105, 1)},
                        new[] {new Gift(2205, 1), new Gift(2189, 1), new Gift(2034, 2), new Gift(2193, 1)}
                    }
                },
                {
                    3118,
                    new[]
                    {
                        new[] {new Gift(2099, 3), new Gift(2100, 3), new Gift(2102, 3)},
                        new[] {new Gift(2206, 1), new Gift(2032, 3)},
                        new[] {new Gift(2206, 1), new Gift(2106, 1), new Gift(2038, 2)},
                        new[] {new Gift(2206, 1), new Gift(2190, 1), new Gift(2039, 2), new Gift(2109, 1)},
                        new[] {new Gift(2206, 1), new Gift(2190, 1), new Gift(2040, 2), new Gift(2194, 1)}
                    }
                },
                {
                    3119,
                    new[]
                    {
                        new[] {new Gift(2099, 3), new Gift(2100, 3), new Gift(2102, 3)},
                        new[] {new Gift(2027, 15), new Gift(2207, 1)},
                        new[] {new Gift(2207, 1), new Gift(2046, 2), new Gift(2191, 1)},
                        new[] {new Gift(2207, 1), new Gift(2047, 2), new Gift(2191, 1), new Gift(2117, 1)},
                        new[] {new Gift(2207, 1), new Gift(2048, 2), new Gift(2191, 1), new Gift(2195, 1)}
                    }
                },
                {
                    3120,
                    new[]
                    {
                        new[] {new Gift(2099, 3), new Gift(2100, 3), new Gift(2102, 3)},
                        new[] {new Gift(2208, 1), new Gift(2017, 10)},
                        new[] {new Gift(2208, 1), new Gift(2192, 1), new Gift(2042, 2)},
                        new[] {new Gift(2208, 1), new Gift(2192, 1), new Gift(2043, 2), new Gift(2118, 1)},
                        new[] {new Gift(2208, 1), new Gift(2192, 1), new Gift(2044, 2), new Gift(2196, 1)}
                    }
                },
                {
                    3121,
                    new[]
                    {
                        new[]
                        {
                            new Gift(2099, 4), new Gift(2100, 4), new Gift(2102, 4), new Gift(1031, 3),
                            new Gift(1032, 3), new Gift(1033, 3), new Gift(1034, 3)
                        },
                        new[] {new Gift(2034, 3), new Gift(2205, 1), new Gift(2189, 1)},
                        new[] {new Gift(2035, 3), new Gift(2193, 1), new Gift(2275, 1)},
                        new[] {new Gift(2036, 3), new Gift(2193, 1), new Gift(1028, 1)},
                        new[]
                        {
                            new Gift(2037, 3), new Gift(2193, 1), new Gift(1028, 1), new Gift(1029, 1),
                            new Gift(2197, 1)
                        }
                    }
                },
                {
                    3122,
                    new[]
                    {
                        new[] {new Gift(2099, 4), new Gift(2100, 4), new Gift(2102, 4), new Gift(2032, 4)},
                        new[] {new Gift(2038, 3), new Gift(2206, 1), new Gift(2190, 1)},
                        new[] {new Gift(2039, 3), new Gift(2194, 1), new Gift(2105, 1)},
                        new[] {new Gift(2040, 3), new Gift(2194, 1), new Gift(1028, 1)},
                        new[]
                        {
                            new Gift(2041, 3), new Gift(2194, 1), new Gift(1028, 1), new Gift(1029, 1),
                            new Gift(2198, 1)
                        }
                    }
                },
                {
                    3123,
                    new[]
                    {
                        new[] {new Gift(2099, 4), new Gift(2100, 4), new Gift(2102, 4), new Gift(2047, 15)},
                        new[] {new Gift(2046, 3), new Gift(2205, 1), new Gift(2189, 1)},
                        new[] {new Gift(2047, 3), new Gift(2195, 1), new Gift(2117, 1)},
                        new[] {new Gift(2048, 3), new Gift(2195, 1), new Gift(1028, 1)},
                        new[]
                        {
                            new Gift(2049, 3), new Gift(2195, 1), new Gift(1028, 1), new Gift(1029, 1),
                            new Gift(2199, 1)
                        }
                    }
                },
                {
                    3124,
                    new[]
                    {
                        new[] {new Gift(2099, 4), new Gift(2100, 4), new Gift(2102, 4), new Gift(2017, 10)},
                        new[] {new Gift(2042, 3), new Gift(2192, 1), new Gift(2189, 1)},
                        new[] {new Gift(2043, 3), new Gift(2196, 1), new Gift(2118, 1)},
                        new[] {new Gift(2044, 3), new Gift(2196, 1), new Gift(1028, 1)},
                        new[]
                        {
                            new Gift(2045, 3), new Gift(2196, 1), new Gift(1028, 1), new Gift(1029, 1),
                            new Gift(2200, 1)
                        }
                    }
                },
                {
                    3125,
                    new[]
                    {
                        new[] {new Gift(2034, 4), new Gift(2189, 2), new Gift(2205, 2)},
                        new[] {new Gift(2035, 4), new Gift(2105, 2)},
                        new[] {new Gift(2036, 4), new Gift(2193, 2)},
                        new[]
                        {
                            new Gift(2037, 4), new Gift(2193, 2), new Gift(2201, 2), new Gift(2226, 2),
                            new Gift(1028, 2), new Gift(1029, 2)
                        },
                        new[]
                        {
                            new Gift(2213, 1), new Gift(2193, 2), new Gift(2034, 2), new Gift(2226, 2),
                            new Gift(1030, 2)
                        }
                    }
                },
                {
                    3126,
                    new[]
                    {
                        new[] {new Gift(2038, 4), new Gift(2106, 2), new Gift(2206, 2)},
                        new[] {new Gift(2039, 4), new Gift(2109, 2)},
                        new[] {new Gift(2040, 4), new Gift(2194, 2)},
                        new[]
                        {
                            new Gift(2040, 4), new Gift(2194, 2), new Gift(2201, 2), new Gift(2231, 2),
                            new Gift(1028, 2), new Gift(1029, 2)
                        },
                        new[]
                        {
                            new Gift(2214, 1), new Gift(2194, 1), new Gift(2231, 2), new Gift(2202, 1),
                            new Gift(1030, 2)
                        }
                    }
                },
                {
                    3127,
                    new[]
                    {
                        new[] {new Gift(2046, 4), new Gift(2207, 2)},
                        new[] {new Gift(2047, 4), new Gift(2117, 2)},
                        new[] {new Gift(2048, 4), new Gift(2195, 2)},
                        new[]
                        {
                            new Gift(2049, 4), new Gift(2195, 2), new Gift(2199, 2), new Gift(1028, 2),
                            new Gift(1029, 2)
                        },
                        new[] {new Gift(2216, 1), new Gift(2195, 2), new Gift(2203, 1), new Gift(1030, 2)}
                    }
                },
                {
                    3128,
                    new[]
                    {
                        new[] {new Gift(2042, 4), new Gift(2192, 2), new Gift(2208, 2)},
                        new[] {new Gift(2043, 4), new Gift(2118, 2)},
                        new[] {new Gift(2044, 4), new Gift(2196, 2)},
                        new[]
                        {
                            new Gift(2045, 4), new Gift(2196, 2), new Gift(2200, 2), new Gift(1028, 2),
                            new Gift(1029, 2)
                        },
                        new[] {new Gift(2215, 1), new Gift(2196, 2), new Gift(2204, 1), new Gift(1030, 2)}
                    }
                },
                {
                    3130,
                    new[]
                    {
                        new[] {new Gift(2042, 4), new Gift(2192, 2), new Gift(2208, 2)},
                        new[] {new Gift(2043, 4), new Gift(2118, 2)},
                        new[] {new Gift(2044, 4), new Gift(2196, 2)},
                        new[]
                        {
                            new Gift(2045, 4), new Gift(2196, 2), new Gift(2200, 2), new Gift(1028, 2),
                            new Gift(1029, 2)
                        },
                        new[] {new Gift(2215, 1), new Gift(2196, 2), new Gift(2204, 1), new Gift(1030, 2)}
                    }
                },
                {
                    3131,
                    new[]
                    {
                        new[] {new Gift(2042, 4), new Gift(2192, 2), new Gift(2208, 2)},
                        new[] {new Gift(2043, 4), new Gift(2118, 2)},
                        new[] {new Gift(2044, 4), new Gift(2196, 2)},
                        new[]
                        {
                            new Gift(2045, 4), new Gift(2196, 2), new Gift(2200, 2), new Gift(1028, 2),
                            new Gift(1029, 2)
                        },
                        new[] {new Gift(2215, 1), new Gift(2196, 2), new Gift(2204, 1), new Gift(1030, 2)}
                    }
                }
            };

        public int[][] MinilandMaxPoint = {
            new[] { 999, 4999, 7999, 11999, 15999, 1000000 },
            new[] { 999, 4999, 9999, 13999, 17999, 1000000 },
            new[] { 999, 3999, 7999, 14999, 24999, 1000000 },
            new[] { 999, 3999, 7999, 11999, 19999, 1000000 },
            new[] { 999, 4999, 7999, 11999, 15999, 1000000 },
            new[] { 999, 4999, 7999, 11999, 15999, 1000000 }
        };

        private MinilandHelper()
        {
        }

        public static MinilandHelper Instance => _instance ??= new MinilandHelper();

        public Gift? GetMinilandGift(short game, int point)
        {
            var rand = new Random();
            return _gifts[game][point].OrderBy(s => rand.Next()).FirstOrDefault();
        }
    }
}
