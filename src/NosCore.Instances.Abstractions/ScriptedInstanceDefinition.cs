//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.Instances.Abstractions
{
    public record InstanceGift(short VNum, short Amount, byte Design = 0, bool RandomRare = false);

    // Where one side of a bout lines up, waits after a fall, and fights. Fighters line up two
    // cells apart below Entry in their fighting order.
    public record BoutSide(Spot Entry, Spot Bench, Spot Ring);

    // The area a side starts in, and comes back to, between two opposite corners.
    public record SideBase(Spot From, Spot To);

    public enum ObjectiveKind
    {
        KillMonster,
        UseObject,
        CollectItem
    }

    // An objective counts kills of the monster, uses of the objects drawn with that vnum before use, or
    // items of that vnum picked up.
    public record InstanceObjective(ObjectiveKind Kind, short VNum, short Count);

    public record InstanceRoom(int Key, short VNum, byte IndexX, byte IndexY, bool KeepsMapMonsters = false)
    {
        public IReadOnlyList<short> MapNpcs { get; init; } = [];
    }

    public record ScriptedInstanceDefinition
    {
        public short Id { get; init; }

        public string? Label { get; init; }

        public string? Title { get; init; }

        public byte LevelMinimum { get; init; }

        public byte LevelMaximum { get; init; }

        public byte TeamSizeMinimum { get; init; }

        public byte TeamSizeMaximum { get; init; }

        public short StartX { get; init; }

        public short StartY { get; init; }

        // The cells after the first that a raid's members are dealt to in joining order, the leader taking the first.
        public IReadOnlyList<Spot> MoreStarts { get; init; } = [];

        public Spot StartOf(int member) =>
            member % (MoreStarts.Count + 1) is var start and > 0 ? MoreStarts[start - 1] : new Spot(StartX, StartY);

        public IReadOnlyList<InstanceGift> RequiredItems { get; init; } = [];

        // Time-spaces a character has to have cleared before entering this one.
        public IReadOnlyList<short> RequiredTimeSpaces { get; init; } = [];

        public IReadOnlyList<InstanceGift> GiftItems { get; init; } = [];

        public IReadOnlyList<InstanceGift> ConsolationItems { get; init; } = [];

        public IReadOnlyList<InstanceGift> DrawItems { get; init; } = [];

        public IReadOnlyList<InstanceGift> SpecialItems { get; init; } = [];

        public long Gold { get; init; }

        // Gold on top of Gold for each monster the run killed.
        public int GoldPerKill { get; init; }

        public long GoldFor(int monstersKilled) => Gold + (long)GoldPerKill * monstersKilled;

        // The gold the stone's record button asks for; a stone without one ignores the button.
        public long? RecordCost { get; init; }

        public int Reputation { get; init; }

        public int FamilyExperience { get; init; }

        // Deaths the party can afford: the one after the last life fails the run.
        public byte Lives { get; init; }

        // The last number of a raid's raidbf and raidmbf. It never changes during a raid: 25 for Mother Cuby,
        // 20 for Zenas. What the client uses it for is not known.
        public byte RaidHudValue { get; init; }

        public IReadOnlyList<BoutSide> Sides { get; init; } = [];

        public IReadOnlyList<SideBase> Bases { get; init; } = [];

        public IReadOnlyList<InstanceRoom> Rooms { get; init; } = [];

        // The exit only lets the party out once every objective is met.
        public IReadOnlyList<InstanceObjective> Objectives { get; init; } = [];
    }
}
