using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Holders;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BattlepassService
{
    public class BattlepassService : IBattlepassService
    {
        private readonly BattlepassHolder _holder;

        public BattlepassService(BattlepassHolder holder)
        {
            _holder = holder;
        }

        public async Task<bool> IncrementQuestObjectives(ICharacterEntity character, long questId, int toAdd)
        {
            var originalQuest = _holder.BattePassQuests.FirstOrDefault(s => s.Id == questId);
            if (originalQuest == null) return false;

            var quest = _holder.BattlepassLogs.Values.FirstOrDefault(s => s.Data == questId && !s.IsItem);

            if (quest == null)
            {
                var questDto = new CharacterBattlepass
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.VisualId,
                    Data = questId,
                    Data2 = toAdd
                };

                _holder.BattlepassLogs.TryAdd(questDto.Id, questDto);
                await character.SendPacketAsync(_holder.BattlepassLogs[questDto.Id].GenerateBpmPacket(_holder, character.VisualId)).ConfigureAwait(false);
                return true;
            }

            var newAdvencement = quest.Data2 + toAdd;
            if (newAdvencement > originalQuest.MaxObjectiveValue) newAdvencement = originalQuest.MaxObjectiveValue;
            var toUpdate = _holder.BattlepassLogs[quest.Id];
            toUpdate.Data2 = newAdvencement;
            await character.SendPacketAsync(toUpdate.GenerateBpmPacket(_holder, character.VisualId)).ConfigureAwait(false);
            return true;
        }
    }
}
