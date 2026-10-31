//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public class ScriptedInstance : ScriptedInstanceDto
    {
        public ScriptedInstanceDefinition? Definition { get; set; }

        public byte EffectiveLevelMinimum =>
            Definition is { LevelMinimum: > 0 } ? Definition.LevelMinimum : LevelMinimum;

        public byte EffectiveLevelMaximum
        {
            get
            {
                var ceiling = Definition is { LevelMaximum: > 0 } ? Definition.LevelMaximum : LevelMaximum;
                return ceiling == 0 ? byte.MaxValue : ceiling;
            }
        }

        public RbrPacket GenerateRbr()
        {
            return new RbrPacket
            {
                TsBasicInfo = new RbrSubPacketBasicInfo
                {
                    TsId = Definition?.Id ?? 0,
                    TsType = IsHeroic ? RbrPacketTsType.HeroMission : RbrPacketTsType.MainMission,
                    TsConditionType = RbrPacketTsConditionType.CanEnterAlone
                },
                Unknown = 0,
                Completed = false,
                MinMaxLevel = new RbrSubPacketMinMaxLevel
                {
                    MinLevel = EffectiveLevelMinimum,
                    MaxLevel = EffectiveLevelMaximum
                },
                RequiredSeeds = (short)(Definition?.RequiredItems.Sum(s => s.Amount) ?? 0),
                DrawRewards = Slots(Definition?.DrawItems, 5),
                SpecialRewards = Slots(Definition?.SpecialItems, 2),
                BonusRewards = Slots(Definition?.GiftItems, 3),
                HighScore = new RbrSubPacketHighScore { Score = 0, Nickname = null },
                IsHidden = false,
                LoserMode = false,
                TitleAndDescription = new RbrSubPacketTitleAndDescription
                {
                    Title = Definition?.Title ?? Definition?.Label ?? string.Empty,
                    Description = Definition?.Label ?? string.Empty
                }
            };
        }

        private static List<RbrSubPacketItem?> Slots(IReadOnlyList<InstanceGift>? gifts, int count)
        {
            var slots = new List<RbrSubPacketItem?>(count);
            for (var i = 0; i < count; i++)
            {
                var gift = gifts != null && i < gifts.Count ? gifts[i] : null;
                slots.Add(new RbrSubPacketItem
                {
                    ItemId = gift?.VNum,
                    Quantity = gift?.Amount ?? 0
                });
            }

            return slots;
        }
    }
}
