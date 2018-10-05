using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NosCore.GameObject.Services;

namespace NosCore.GameObject.Services.Randomizer
{


    public class RandomizerService : ISingletonService
    {
        private static int _seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> Random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));
        public int RandomNumber(int min = 0, int max = 100)
        {
            return Random.Value.Next(min, max);
        }
    }
}
