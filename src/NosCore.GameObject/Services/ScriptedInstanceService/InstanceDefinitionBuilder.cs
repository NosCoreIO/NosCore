//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public sealed class InstanceDefinitionBuilder
    {
        private readonly List<InstanceRoom> _rooms = [];
        private readonly List<InstanceGift> _required = [];
        private readonly List<InstanceGift> _draw = [];
        private readonly List<InstanceGift> _special = [];
        private readonly List<InstanceGift> _gift = [];

        private byte _id;
        private string? _label;
        private string? _title;
        private byte _levelMinimum;
        private byte _levelMaximum;
        private byte _lives;
        private short _startX;
        private short _startY;
        private long _gold;
        private int _reputation;
        private int _familyExperience;

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

        public InstanceDefinitionBuilder WithLives(byte lives)
        {
            _lives = lives;
            return this;
        }

        public InstanceDefinitionBuilder StartingAt(short x, short y)
        {
            _startX = x;
            _startY = y;
            return this;
        }

        public InstanceDefinitionBuilder Rewarding(long gold = 0, int reputation = 0, int familyExperience = 0)
        {
            _gold = gold;
            _reputation = reputation;
            _familyExperience = familyExperience;
            return this;
        }

        public InstanceDefinitionBuilder WithRoom(short mapVNum, out int key, byte indexX = 0, byte indexY = 0)
        {
            key = _rooms.Count + 1;
            _rooms.Add(new InstanceRoom(key, mapVNum, indexX, indexY));
            return this;
        }

        public InstanceDefinitionBuilder Requiring(short vNum, short amount)
        {
            _required.Add(new InstanceGift(vNum, amount));
            return this;
        }

        public InstanceDefinitionBuilder Drawing(short vNum, short amount, short design = 0, bool randomRare = false)
        {
            _draw.Add(new InstanceGift(vNum, amount, design, randomRare));
            return this;
        }

        public InstanceDefinitionBuilder WithSpecialReward(short vNum, short amount, bool heroic = false)
        {
            _special.Add(new InstanceGift(vNum, amount, 0, false, heroic));
            return this;
        }

        public InstanceDefinitionBuilder WithReward(short vNum, short amount)
        {
            _gift.Add(new InstanceGift(vNum, amount));
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
                Lives = _lives,
                StartX = _startX,
                StartY = _startY,
                Gold = _gold,
                Reputation = _reputation,
                FamilyExperience = _familyExperience,
                RequiredItems = _required,
                DrawItems = _draw,
                SpecialItems = _special,
                GiftItems = _gift,
                Rooms = _rooms
            };
        }
    }
}
