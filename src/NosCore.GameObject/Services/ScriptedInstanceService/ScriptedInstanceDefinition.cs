//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public record InstanceGift(short VNum, short Amount, short Design = 0,
        bool IsRandomRare = false, bool IsHeroic = false);

    public record InstanceRoom(int Key, short VNum, byte IndexX, byte IndexY);

    public record ScriptedInstanceDefinition
    {
        public byte Id { get; init; }

        public string? Label { get; init; }

        public string? Title { get; init; }

        public byte LevelMinimum { get; init; }

        public byte LevelMaximum { get; init; }

        public byte Lives { get; init; }

        public short StartX { get; init; }

        public short StartY { get; init; }

        public long Gold { get; init; }

        public int Reputation { get; init; }

        public int FamilyExperience { get; init; }

        public IReadOnlyList<InstanceGift> RequiredItems { get; init; } = [];

        public IReadOnlyList<InstanceGift> DrawItems { get; init; } = [];

        public IReadOnlyList<InstanceGift> SpecialItems { get; init; } = [];

        public IReadOnlyList<InstanceGift> GiftItems { get; init; } = [];

        public IReadOnlyList<InstanceRoom> Rooms { get; init; } = [];
    }
}
