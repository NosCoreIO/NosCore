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

        private byte _id;
        private string? _label;
        private string? _title;
        private byte _levelMinimum;
        private byte _levelMaximum;
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

        public ScriptedInstanceDefinition Build()
        {
            return new ScriptedInstanceDefinition
            {
                Id = _id,
                Label = _label,
                Title = _title,
                LevelMinimum = _levelMinimum,
                LevelMaximum = _levelMaximum,
                StartX = _startX,
                StartY = _startY,
                RequiredItems = [.. _required],
                Rooms = [.. _rooms]
            };
        }
    }
}
