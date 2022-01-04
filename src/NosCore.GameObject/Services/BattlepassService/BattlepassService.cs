using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.ComponentEntities.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BattlepassService
{
    public class BattlepassService : IBattlepassService
    {
        private readonly IDao<BattlepassQuestDto, long> _battlePassQuestDao;

        public BattlepassService(IDao<BattlepassQuestDto, long> battlePassQuestDao)
        {
            _battlePassQuestDao = battlePassQuestDao;
        }

        public async Task IncrementQuestObjectives(ICharacterEntity character, long questId, int toAdd)
        {
            var originalQuest = await _battlePassQuestDao.FirstOrDefaultAsync(s => s.Id == questId);
            if (originalQuest == null) return;

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
                // TODO : to fix
                //await character.SendPacketAsync(character.BattlepassLogs[questDto.Id].GenerateBpmPacket(_holder, character.VisualId)).ConfigureAwait(false);
                return;
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
        }
    }
}
