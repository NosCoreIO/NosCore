//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Persistence;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SaveService
{
    public class SaveService(IDao<CharacterDto, long> characterDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IDao<AccountDto, long> accountDao,
            IDao<StaticBonusDto, long> staticBonusDao,
            IDao<QuicklistEntryDto, Guid> quicklistEntriesDao, IDao<MinilandDto, Guid> minilandDao,
            IMinilandService minilandProvider, IDao<TitleDto, Guid> titleDao,
            IDao<CharacterQuestDto, Guid> characterQuestDao,
            IDao<CharacterQuestObjectiveDto, Guid> characterQuestObjectiveDao,
            IDao<RespawnDto, long> respawnDao, ILogger<SaveService> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            IDaoTransactionScope daoTransactionScope)
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
                // MapX/Y is the last BaseMap position; keep it if we're saving off a BaseMap
                // so miniland-exit can restore it.
                if (character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    characterDto.MapX = character.PositionX;
                    characterDto.MapY = character.PositionY;
                }
                characterDto.SpPoint = character.SpPoint;
                characterDto.SpAdditionPoint = character.SpAdditionPoint;
                characterDto.CurrentScriptId = character.CurrentScriptId;

                // Every DAO call below shares this scope's transaction: the DAOs swallow
                // their own exceptions and report failure through their return value, so
                // each result is checked and the commit only happens when all of them
                // succeeded. Returning early rolls the whole save back.
                void Fail(string operation)
                {
                    logger.LogError(
                        new InvalidOperationException($"{operation} failed; character save rolled back."),
                        logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], characterId);
                }

                await using var transaction = daoTransactionScope.Begin();

                if (await accountDao.TryInsertOrUpdateAsync(account) == null)
                {
                    Fail("Account upsert");
                    return;
                }

                if (await characterDao.TryInsertOrUpdateAsync(characterDto) == null)
                {
                    Fail("Character upsert");
                    return;
                }

                var quicklistEntriesToDelete = quicklistEntriesDao
                        .Where(i => i.CharacterId == characterId)!.ToList()
                    .Where(i => quicklistEntries.All(o => o.Id != i.Id)).ToList();
                if (await quicklistEntriesDao.TryDeleteAsync(quicklistEntriesToDelete.Select(s => s.Id).ToArray()) == null)
                {
                    Fail("QuicklistEntry delete");
                    return;
                }
                if (!await quicklistEntriesDao.TryInsertOrUpdateAsync(quicklistEntries))
                {
                    Fail("QuicklistEntry upsert");
                    return;
                }

                var itemsToDelete = inventoryItemInstanceDao
                        .Where(i => i.CharacterId == characterId)!.ToList()
                    .Where(i => inventoryService.Values.All(o => o.Id != i.Id)).ToList();

                // Inventory delete order: child rows first, then parent ItemInstance rows.
                if (await inventoryItemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.Id).ToArray()) == null)
                {
                    Fail("InventoryItemInstance delete");
                    return;
                }
                if (await itemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.ItemInstanceId).ToArray()) == null)
                {
                    Fail("ItemInstance delete");
                    return;
                }

                // Inventory insert order: parent ItemInstance rows first so the FK on
                // InventoryItemInstance.ItemInstanceId resolves on insert.
                if (!await itemInstanceDao.TryInsertOrUpdateAsync(inventoryService.Values.Select(s => s.ItemInstance).ToArray()))
                {
                    Fail("ItemInstance upsert");
                    return;
                }
                if (!await inventoryItemInstanceDao.TryInsertOrUpdateAsync(inventoryService.Values.ToArray()))
                {
                    Fail("InventoryItemInstance upsert");
                    return;
                }

                var staticBonusToDelete = staticBonusDao
                        .Where(i => i.CharacterId == characterId)!.ToList()
                    .Where(i => staticBonusList.All(o => o.StaticBonusId != i.StaticBonusId)).ToList();
                if (await staticBonusDao.TryDeleteAsync(staticBonusToDelete.Select(s => s.StaticBonusId)) == null)
                {
                    Fail("StaticBonus delete");
                    return;
                }
                if (!await staticBonusDao.TryInsertOrUpdateAsync(staticBonusList))
                {
                    Fail("StaticBonus upsert");
                    return;
                }

                if (!await titleDao.TryInsertOrUpdateAsync(titles))
                {
                    Fail("Title upsert");
                    return;
                }

                var minilandDto = (MinilandDto)minilandProvider.GetMiniland(characterId);
                if (await minilandDao.TryInsertOrUpdateAsync(minilandDto) == null)
                {
                    Fail("Miniland upsert");
                    return;
                }

                var questsToDelete = characterQuestDao
                        .Where(i => i.CharacterId == characterId)!.ToList()
                    .Where(i => quests.Values.All(o => o.QuestId != i.QuestId)).ToList();
                if (await characterQuestDao.TryDeleteAsync(questsToDelete.Select(s => s.Id)) == null)
                {
                    Fail("CharacterQuest delete");
                    return;
                }
                if (!await characterQuestDao.TryInsertOrUpdateAsync(quests.Values))
                {
                    Fail("CharacterQuest upsert");
                    return;
                }

                var liveObjectives = quests.Values.SelectMany(q =>
                    q.ObjectiveProgress.Select(kv => new CharacterQuestObjectiveDto
                    {
                        Id = Guid.NewGuid(),
                        CharacterQuestId = q.Id,
                        QuestObjectiveId = kv.Key,
                        Count = kv.Value
                    })).ToList();
                var liveQuestIds = quests.Values.Select(q => q.Id).ToHashSet();
                var existingObjectives = characterQuestObjectiveDao
                    .Where(o => liveQuestIds.Contains(o.CharacterQuestId))?.ToList() ?? new List<CharacterQuestObjectiveDto>();
                var liveObjectiveKeys = liveObjectives
                    .Select(o => (o.CharacterQuestId, o.QuestObjectiveId)).ToHashSet();
                var objectivesToDelete = existingObjectives
                    .Where(o => !liveObjectiveKeys.Contains((o.CharacterQuestId, o.QuestObjectiveId)))
                    .Select(o => o.Id).ToList();
                foreach (var live in liveObjectives)
                {
                    var match = existingObjectives.FirstOrDefault(o =>
                        o.CharacterQuestId == live.CharacterQuestId && o.QuestObjectiveId == live.QuestObjectiveId);
                    if (match != null)
                    {
                        live.Id = match.Id;
                    }
                }
                if (await characterQuestObjectiveDao.TryDeleteAsync(objectivesToDelete) == null)
                {
                    Fail("CharacterQuestObjective delete");
                    return;
                }
                if (!await characterQuestObjectiveDao.TryInsertOrUpdateAsync(liveObjectives))
                {
                    Fail("CharacterQuestObjective upsert");
                    return;
                }

                if (!await respawnDao.TryInsertOrUpdateAsync(character.Respawns))
                {
                    Fail("Respawn upsert");
                    return;
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], session.Character.CharacterId);
            }
        }
    }
}
