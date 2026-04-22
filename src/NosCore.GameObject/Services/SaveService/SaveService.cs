//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Shared.I18N;
using Serilog;
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
            IDao<RespawnDto, long> respawnDao, ILogger logger,
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

                // Inventory delete order: child rows first, then parent ItemInstance rows.
                await inventoryItemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.Id).ToArray());
                await itemInstanceDao.TryDeleteAsync(itemsToDelete.Select(s => s.ItemInstanceId).ToArray());

                // Inventory insert order: parent ItemInstance rows first so the FK on
                // InventoryItemInstance.ItemInstanceId resolves on insert. The DAO swallows
                // exceptions and returns false on failure, so we MUST check the result —
                // otherwise a silent failure on the ItemInstance insert cascades into a
                // confusing FK-violation error on the InventoryItemInstance insert that
                // follows. Skipping the child insert keeps the failure mode loud and
                // localized to the actual broken layer.
                var itemInstancesSaved = await itemInstanceDao
                    .TryInsertOrUpdateAsync(inventoryService.Values.Select(s => s.ItemInstance).ToArray());
                if (!itemInstancesSaved)
                {
                    logger.Error(
                        new InvalidOperationException("ItemInstance batch insert failed; skipping InventoryItemInstance to avoid FK cascade."),
                        logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], session.Character.CharacterId);
                    return;
                }
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
                var questsSaved = await characterQuestDao.TryInsertOrUpdateAsync(quests.Values);
                if (!questsSaved)
                {
                    logger.Error(
                        new InvalidOperationException("CharacterQuest upsert failed; skipping objective upsert to avoid FK cascade."),
                        logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], characterId);
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
                var objectivesDeleted = await characterQuestObjectiveDao.TryDeleteAsync(objectivesToDelete);
                if (objectivesDeleted == null)
                {
                    logger.Error(
                        new InvalidOperationException("CharacterQuestObjective delete failed; skipping objective upsert to avoid orphaned-row conflicts on next save."),
                        logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], characterId);
                    return;
                }
                var objectivesSaved = await characterQuestObjectiveDao.TryInsertOrUpdateAsync(liveObjectives);
                if (!objectivesSaved)
                {
                    logger.Error(
                        new InvalidOperationException("CharacterQuestObjective upsert failed; quest progress will reset on reconnect."),
                        logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], characterId);
                    return;
                }

                await respawnDao.TryInsertOrUpdateAsync(character.Respawns);
            }
            catch (Exception e)
            {
                logger.Error(e, logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], session.Character.CharacterId);
            }
        }
    }
}
