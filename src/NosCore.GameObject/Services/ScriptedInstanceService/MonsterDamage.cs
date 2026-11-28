//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    // One player's hits on a monster. The score adds each hit's damage divided by ten, rounded down hit by hit:
    // official traces show hits of 2047 and 5768 scored 780, not 781.
    public readonly record struct PlayerDamage(long CharacterId, long Damage, long Score);

    // What the players' own hits did to one monster of a run, for the damage ranking shown when a raid boss dies.
    // Every hit counts in full, the killing blow too, even past the HP the monster had left.
    public sealed class MonsterDamage(int maxHp)
    {
        private const int ScoreDivisor = 10;

        private readonly Lock _lock = new();
        private readonly Dictionary<long, PlayerDamage> _byPlayer = [];

        public int MaxHp { get; } = maxHp;

        public void Add(long characterId, int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            lock (_lock)
            {
                var hits = _byPlayer.GetValueOrDefault(characterId, new PlayerDamage(characterId, 0, 0));
                _byPlayer[characterId] = hits with { Damage = hits.Damage + damage, Score = hits.Score + damage / ScoreDivisor };
            }
        }

        // Highest damage first.
        public IReadOnlyList<PlayerDamage> Ranking()
        {
            lock (_lock)
            {
                return _byPlayer.Values.OrderByDescending(s => s.Damage).ThenBy(s => s.CharacterId).ToList();
            }
        }

        // The player's damage as a share of the monster's maximum HP, rounded down.
        public byte PercentOf(PlayerDamage damage) =>
            MaxHp <= 0 ? (byte)0 : (byte)Math.Min(byte.MaxValue, damage.Damage * 100 / MaxHp);
    }
}
