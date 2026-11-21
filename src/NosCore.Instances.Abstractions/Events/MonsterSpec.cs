//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Instances.Abstractions
{
    internal sealed record MonsterSpec(short VNum) : IMonster
    {
        private Placement? Place { get; init; }

        private int Copies { get; init; } = 1;

        private bool IsBonus { get; init; }

        private Action<RoomEvent>? Death { get; init; }

        private Action<RoomEvent>? Hit { get; init; }

        private IReadOnlyList<(byte Percent, Action<RoomEvent> Step)> HpSteps { get; init; } = [];

        internal static IReadOnlyList<MonsterSpec> All(IMonster[] monsters) => monsters.Select(Of).ToList();

        private static MonsterSpec Of(IMonster monster)
        {
            var spec = monster as MonsterSpec
                ?? throw new ArgumentException($"{monster} is not a monster of this script.", nameof(monster));
            return spec.Place == null
                ? throw new InvalidOperationException($"Monster {spec.VNum} has no place: give it At, Anywhere or Around.")
                : spec;
        }

        public IMonster At(short x, short y) => this with { Place = new Placement(new Spot(x, y), null) };

        public IMonster Anywhere() => this with { Place = new Placement(null, null) };

        public IMonster Around(short x, short y, short radius)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(radius);
            return this with { Place = new Placement(new Spot(x, y), radius) };
        }

        public IMonster Count(int copies)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(copies, 1);
            return this with { Copies = copies };
        }

        public IMonster Bonus() => this with { IsBonus = true };

        public IMonster OnDeath(Action<RoomEvent> step) => this with { Death = Death + step };

        public IMonster OnHit(Action<RoomEvent> step) => this with { Hit = Hit + step };

        public IMonster OnHpBelow(byte percent, Action<RoomEvent> step)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(percent, (byte)1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(percent, (byte)99);
            return this with { HpSteps = [.. HpSteps, (percent, step)] };
        }

        // Each copy is placed on its own: copies sent Anywhere or Around draw their cells independently and may share one.
        internal IEnumerable<MonsterSpawn> Spawns(IInstanceRun run, RoomHandle room)
        {
            var death = Bind(Death, room);
            var hit = Bind(Hit, room);
            var thresholds = HpSteps.Count == 0
                ? null
                : HpSteps.OrderByDescending(s => s.Percent)
                    .Select(s => new HpThreshold(s.Percent, r => s.Step(new RoomEvent(r, room))))
                    .ToList();
            for (var copy = 0; copy < Copies; copy++)
            {
                yield return new MonsterSpawn(VNum, PlaceIn(run, room), IsBonus: IsBonus, OnDeath: death, OnHit: hit,
                    OnHpBelow: thresholds);
            }
        }

        private Spot PlaceIn(IInstanceRun run, RoomHandle room) => Place switch
        {
            { At: { } around, Radius: { } radius } => run.RandomCell(room.Key, around, radius),
            { At: { } at } => at,
            _ => run.RandomCell(room.Key)
        };

        private static Action<IInstanceRun>? Bind(Action<RoomEvent>? step, RoomHandle room) =>
            step == null ? null : run => step(new RoomEvent(run, room));

        private sealed record Placement(Spot? At, short? Radius);
    }

    internal sealed record NpcSpec(short VNum) : INpc
    {
        private Spot? Place { get; init; }

        internal static NpcSpec Of(INpc npc)
        {
            var spec = npc as NpcSpec ?? throw new ArgumentException($"{npc} is not an NPC of this script.", nameof(npc));
            return spec.Place == null ? throw new InvalidOperationException($"NPC {spec.VNum} has no place: give it At.") : spec;
        }

        public INpc At(short x, short y) => this with { Place = new Spot(x, y) };

        internal NpcSpawn Spawn() => new(VNum, Place ?? default);
    }
}
