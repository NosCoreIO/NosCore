//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;

namespace NosCore.GameObject.Services.DigestionService
{
    // What is still owed on each of the next five ticks. An item spreads its value evenly over
    // the five, so overlapping items simply add up slot by slot.
    public sealed class DigestionSchedule
    {
        public const int TickCount = 5;

        private readonly int[] _hp = new int[TickCount];
        private readonly int[] _mp = new int[TickCount];

        public bool IsEmpty => _hp.All(v => v == 0) && _mp.All(v => v == 0);

        public void Add(int hpPerTick, int mpPerTick)
        {
            for (var i = 0; i < TickCount; i++)
            {
                _hp[i] += hpPerTick;
                _mp[i] += mpPerTick;
            }
        }

        public bool WouldExceed(int hpPerTick, int mpPerTick, int cap) =>
            Enumerable.Range(0, TickCount).Any(i => _hp[i] + hpPerTick > cap || _mp[i] + mpPerTick > cap);

        public bool WouldReach(int hpPerTick, int mpPerTick, int limit) =>
            !IsEmpty && Enumerable.Range(0, TickCount).Any(i => _hp[i] + hpPerTick >= limit || _mp[i] + mpPerTick >= limit);

        public (int Hp, int Mp) Tick(int cap = int.MaxValue)
        {
            var due = (Math.Min(_hp[0], cap), Math.Min(_mp[0], cap));
            Array.Copy(_hp, 1, _hp, 0, TickCount - 1);
            Array.Copy(_mp, 1, _mp, 0, TickCount - 1);
            _hp[TickCount - 1] = 0;
            _mp[TickCount - 1] = 0;
            return due;
        }

        public void Clear()
        {
            Array.Clear(_hp);
            Array.Clear(_mp);
        }
    }
}
