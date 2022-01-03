using NosCore.Data.Dto;
using NosCore.GameObject.Holders;
using NosCore.Packets.ClientPackets.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.BattlepassService
{
    public class CharacterBattlepass : CharacterBattlepassDto
    {
        public BpmPacket GenerateBpmPacket(BattlepassHolder holder, long characterId)
        {
            List<BpmSubTypePacket> subPackets = new();
            foreach (var quest in holder.BattePassQuests)
            {
                var characterAdvencement = holder.BattlepassLogs.Values.FirstOrDefault(s => s.CharacterId == characterId && s.Data == Data);
                long actualAdvencement = characterAdvencement == null ? 0 : Convert.ToInt64(characterAdvencement.Data2);
                subPackets.Add(new()
                {
                    QuestId = quest.Id,
                    MissionType = quest.MissionType,
                    FrequencyType = quest.FrequencyType,
                    Advencement = actualAdvencement,
                    MaxObjectiveValue = quest.MaxObjectiveValue,
                    Reward = (byte)quest.RewardAmount,
                    MissionMinutesRemaining = 1 // TODO
                });
            }

            return new BpmPacket
            {
                IsBattlePassIconEnabled = true, // TODO
                MaxBattlePassPoints = 150000, // TOOD
                QuestList = subPackets
            };
        }
    }
}
