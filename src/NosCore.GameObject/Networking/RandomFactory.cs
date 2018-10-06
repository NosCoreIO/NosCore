using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NosCore.GameObject.Networking
{
    public class RandomFactory
    {
        private static RandomFactory _instance;

        private RandomFactory()
        {
        }

        public static RandomFactory Instance => _instance ?? (_instance = new RandomFactory());
        private static int _seed = Environment.TickCount;
        private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));
        public int RandomNumber(int min = 0, int max = 100)
        {
            return _random.Value.Next(min, max);
        }

    }
}
