using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.Services.BattlepassService;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace NosCore.GameObject.Holders
{
    public class BattlepassHolder
    {
        public BattlepassHolder(IDao<CharacterBattlepassDto, Guid> characterBattlePassDao, IDao<BattlepassItemDto, Guid> battlePassItemDao, IDao<BattlepassQuestDto, long> battlePassQuestDao)
        {
            var logs = characterBattlePassDao.LoadAll();
            BattlepassLogs = new(logs.ToDictionary(x => x.Id, x => new CharacterBattlepass
            {
                Id = x.Id,
                CharacterId = x.CharacterId,
                Data = x.Data,
                Data2 = x.Data2,
                Data3 = x.Data3,
                IsItem = x.IsItem
            }));

            var items = battlePassItemDao.LoadAll();
            BattePassItems = new(items);

            var quests = battlePassQuestDao.LoadAll();
            BattePassQuests = new(quests);
        }

        public ConcurrentDictionary<Guid, CharacterBattlepass> BattlepassLogs { get; set; }

        public ConcurrentBag<BattlepassItemDto> BattePassItems { get; set; }

        public ConcurrentBag<BattlepassQuestDto> BattePassQuests { get; set; }
    }
}
