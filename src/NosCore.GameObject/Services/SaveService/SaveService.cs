//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SaveService
{
    public class SaveService(IDao<CharacterDto, long> characterDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IDao<AccountDto, long> accountDao,
            IDao<StaticBonusDto, long> staticBonusDao,
            IDao<QuicklistEntryDto, Guid> quicklistEntriesDao, IDao<MinilandDto, Guid> minilandDao,
            IMinilandService minilandProvider, IDao<TitleDto, Guid> titleDao,
            IDao<CharacterQuestDto, Guid> characterQuestDao, ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : ISaveService
    {
        public async Task SaveAsync(ClientSession session)
        {
            try
            {
                var character = session.Character;
                var characterId = character.CharacterId;
                var account = character.Account;
                var characterDto = character.CharacterDto;
                var quicklistEntries = character.QuicklistEntries;
                var inventoryService = character.InventoryService;
                var staticBonusList = character.StaticBonusList;
                var titles = character.Titles;
                var quests = character.Quests;

                characterDto.Hp = character.Hp;
                characterDto.Mp = character.Mp;
                characterDto.Level = character.Level;
                characterDto.LevelXp = character.LevelXp;
                characterDto.JobLevel = character.JobLevel;
                characterDto.JobLevelXp = character.JobLevelXp;
                characterDto.HeroLevel = character.HeroLevel;
                characterDto.HeroXp = character.HeroXp;
                characterDto.Gold = character.Gold;
                characterDto.Reput = character.Reput;
                characterDto.Dignity = character.Dignity;
                characterDto.Compliment = character.Compliment;
                characterDto.MapX = character.PositionX;
                characterDto.MapY = character.PositionY;
                characterDto.SpPoint = character.SpPoint;
                characterDto.SpAdditionPoint = character.SpAdditionPoint;

                await accountDao.TryInsertOrUpdateAsync(account);
                await characterDao.TryInsertOrUpdateAsync(characterDto);

                var quicklistEntriesToDelete = quicklistEntriesDao
                        .Where(i => i.CharacterId == characterId)!.ToList()
                    .Where(i => quicklistEntries.All(o => o.Id != i.Id)).ToList();
                await quicklistEntriesDao.TryDeleteAsync(quicklistEntriesToDelete.Select(s => s.Id).ToArray());
                await quicklistEntriesDao.TryInsertOrUpdateAsync(quicklistEntries);

                var itemsToDelete = inventoryItemInstanceDao
                        .Where(i => i.CharacterId == characterId)!.ToList()
                    .Where(i => inventoryService.Values.All(o => o.Id != i.Id)).ToList();

                await inventoryItemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.Id).ToArray());
                await itemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.ItemInstanceId).ToArray());

                await itemInstanceDao.TryInsertOrUpdateAsync(inventoryService.Values.Select(s => s.ItemInstance).ToArray());
                await inventoryItemInstanceDao.TryInsertOrUpdateAsync(inventoryService.Values.ToArray());

                var staticBonusToDelete = staticBonusDao
                        .Where(i => i.CharacterId == characterId)!.ToList()
                    .Where(i => staticBonusList.All(o => o.StaticBonusId != i.StaticBonusId)).ToList();
                await staticBonusDao.TryDeleteAsync(staticBonusToDelete.Select(s => s.StaticBonusId));
                await staticBonusDao.TryInsertOrUpdateAsync(staticBonusList);

                await titleDao.TryInsertOrUpdateAsync(titles);

                var minilandDto = (MinilandDto)minilandProvider.GetMiniland(characterId);
                await minilandDao.TryInsertOrUpdateAsync(minilandDto);

                var questsToDelete = characterQuestDao
                        .Where(i => i.CharacterId == characterId)!.ToList()
                    .Where(i => quests.Values.All(o => o.QuestId != i.QuestId)).ToList();
                await characterQuestDao.TryDeleteAsync(questsToDelete.Select(s => s.Id));
                await characterQuestDao.TryInsertOrUpdateAsync(quests.Values);
            }
            catch (Exception e)
            {
                logger.Error(logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], session.Character.CharacterId, e);
            }
        }
    }
}
