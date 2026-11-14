//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.Instances.Abstractions
{
    public record InstanceGift(short VNum, short Amount);

    public record InstanceRoom(int Key, short VNum, byte IndexX, byte IndexY, bool KeepsMapMonsters = false);

    public record ScriptedInstanceDefinition
    {
        public byte Id { get; init; }

        public string? Label { get; init; }

        public string? Title { get; init; }

        public byte LevelMinimum { get; init; }

        public byte LevelMaximum { get; init; }

        public byte TeamSizeMinimum { get; init; }

        public byte TeamSizeMaximum { get; init; }

        public short StartX { get; init; }

        public short StartY { get; init; }

        public IReadOnlyList<InstanceGift> RequiredItems { get; init; } = [];

        public IReadOnlyList<InstanceGift> GiftItems { get; init; } = [];

        public IReadOnlyList<InstanceRoom> Rooms { get; init; } = [];
    }
}
