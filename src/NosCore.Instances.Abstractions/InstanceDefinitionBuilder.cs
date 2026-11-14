//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.Instances.Abstractions
{
    public sealed class InstanceDefinitionBuilder
    {
        private readonly List<InstanceRoom> _rooms = [];
        private readonly List<InstanceGift> _required = [];
        private readonly List<InstanceGift> _gifts = [];
        private readonly List<InstanceGift> _consolations = [];
        private readonly List<BoutSide> _sides = [];

        private byte _id;
        private string? _label;
        private string? _title;
        private byte _levelMinimum;
        private byte _levelMaximum;
        private byte _teamSizeMinimum;
        private byte _teamSizeMaximum;
        private short _startX;
        private short _startY;

        public static InstanceDefinitionBuilder Named(byte id, string label, string title)
        {
            return new InstanceDefinitionBuilder { _id = id, _label = label, _title = title };
        }

        public InstanceDefinitionBuilder ForLevels(byte minimum, byte maximum)
        {
            _levelMinimum = minimum;
            _levelMaximum = maximum;
            return this;
        }

        public InstanceDefinitionBuilder ForTeams(byte minimum, byte maximum)
        {
            _teamSizeMinimum = minimum;
            _teamSizeMaximum = maximum;
            return this;
        }

        public InstanceDefinitionBuilder StartingAt(short x, short y)
        {
            _startX = x;
            _startY = y;
            return this;
        }

        public InstanceDefinitionBuilder WithRoom(short mapVNum, out int key, byte indexX = 0, byte indexY = 0)
        {
            key = _rooms.Count + 1;
            _rooms.Add(new InstanceRoom(key, mapVNum, indexX, indexY));
            return this;
        }

        // A room that comes with the monsters its map has outside the instance.
        public InstanceDefinitionBuilder WithMapRoom(short mapVNum, out int key)
        {
            key = _rooms.Count + 1;
            _rooms.Add(new InstanceRoom(key, mapVNum, 0, 0, KeepsMapMonsters: true));
            return this;
        }

        public InstanceDefinitionBuilder Requiring(short vNum, short amount)
        {
            _required.Add(new InstanceGift(vNum, amount));
            return this;
        }

        // What every player still inside receives when the run succeeds; between two sides, what the winners receive.
        public InstanceDefinitionBuilder WithReward(short vNum, short amount)
        {
            _gifts.Add(new InstanceGift(vNum, amount));
            return this;
        }

        // What the losing side receives when a run between two sides ends.
        public InstanceDefinitionBuilder WithConsolation(short vNum, short amount)
        {
            _consolations.Add(new InstanceGift(vNum, amount));
            return this;
        }

        // Sides are taken in order: the first is Zenas, the second Erenia.
        public InstanceDefinitionBuilder WithSide(Spot entry, Spot bench, Spot ring)
        {
            _sides.Add(new BoutSide(entry, bench, ring));
            return this;
        }

        public ScriptedInstanceDefinition Build()
        {
            return new ScriptedInstanceDefinition
            {
                Id = _id,
                Label = _label,
                Title = _title,
                LevelMinimum = _levelMinimum,
                LevelMaximum = _levelMaximum,
                TeamSizeMinimum = _teamSizeMinimum,
                TeamSizeMaximum = _teamSizeMaximum,
                StartX = _startX,
                StartY = _startY,
                RequiredItems = [.. _required],
                GiftItems = [.. _gifts],
                ConsolationItems = [.. _consolations],
                Sides = [.. _sides],
                Rooms = [.. _rooms]
            };
        }
    }
}
