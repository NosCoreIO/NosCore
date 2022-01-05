using NosCore.Data.Dto;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BattlepassService
{
    public class BattlepassService : IBattlepassService
    {
        private readonly List<BattlepassQuestDto> _quests;

        public BattlepassService(List<BattlepassQuestDto> quests)
        {
            _quests = quests;
        }

        public Task IncrementQuestObjectives(ICharacterEntity character, long questId, int toAdd)
        {
            var originalQuest = _quests.FirstOrDefault(s => s.Id == questId);
            if (originalQuest == null) return Task.CompletedTask;

            var quest = character.BattlepassLogs.Values.FirstOrDefault(s => s.Data == questId && !s.IsItem);

            if (quest == null)
            {
                var questDto = new CharacterBattlepassDto
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.VisualId,
                    Data = questId,
                    Data2 = toAdd
                };

                character.BattlepassLogs.TryAdd(questDto.Id, questDto);
                await character.SendPacketAsync(character.GenerateBpmPacket(_quests, toAdd)).ConfigureAwait(false);
                return Task.CompletedTask;
            }

            var newAdvencement = quest.Data2 + toAdd;
            if (newAdvencement > originalQuest.MaxObjectiveValue)
            {
                newAdvencement = originalQuest.MaxObjectiveValue;
            }
            var toUpdate = character.BattlepassLogs[quest.Id];
            toUpdate.Data2 = newAdvencement;
            // TODO : to fix
            //await character.SendPacketAsync(toUpdate.GenerateBpmPacket(_holder, character.VisualId)).ConfigureAwait(false);
            return Task.CompletedTask;
        }
    }
}
