//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Algorithm.SpeedService;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Networking.SessionGroup;
using NosCore.Shared.I18N;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.CharacterService
{
    public class CharacterLoadService(ILoggerFactory loggerFactory, IItemGenerationService itemProvider,
            IMapInstanceAccessorService mapInstanceAccessorService, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IDao<StaticBonusDto, long> staticBonusDao,
            IDao<QuicklistEntryDto, Guid> quickListEntriesDao, IDao<TitleDto, Guid> titleDao,
            IDao<CharacterQuestDto, Guid> characterQuestDao,
            IDao<CharacterQuestObjectiveDto, Guid> characterQuestObjectiveDao,
            IDao<RespawnDto, long> respawnDao,
            IDao<CompletedTimeSpaceDto, Guid> completedTimeSpaceDao,
            IDao<ScriptDto, Guid> scriptDao, IQuestCatalog questCatalog,
            IOptions<WorldConfiguration> configuration, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            IItemCatalog itemCatalog, IHpService hpService, IMpService mpService, ISpeedService speedService,
            IVitalityService vitalityService, ISessionGroupFactory sessionGroupFactory,
            IMateService mateService, IFamilyService familyService, IStaticBuffService staticBuffService)
        : ICharacterLoadService
    {
        public async Task<PlayerComponentBundle> LoadAsync(ClientSession session, CharacterDto character)
        {
            var characterId = character.CharacterId;
            if (mapInstanceAccessorService.GetArenaById(character.MapId)?.Portals.FirstOrDefault() is { } arenaExit)
            {
                character.MapId = arenaExit.DestinationMapId;
                character.MapX = arenaExit.DestinationX;
                character.MapY = arenaExit.DestinationY;
            }

            var mapInstance = mapInstanceAccessorService.GetBaseMapById(character.MapId)
                ?? throw new InvalidOperationException($"Map {character.MapId} is not loaded.");
            var script = character.CurrentScriptId != null
                ? await scriptDao.FirstOrDefaultAsync(s => s.Id == character.CurrentScriptId)
                : null;

            var inventories = inventoryItemInstanceDao
                .Where(s => s.CharacterId == characterId)
                ?.ToList() ?? new List<InventoryItemInstanceDto>();
            var ids = inventories.Select(o => o.ItemInstanceId).ToList();
            var itemInstances = (itemInstanceDao.Where(s => ids.Contains(s.Id)) ?? []).OfType<IItemInstanceDto>().ToList();

            var inventoryService = new InventoryService.InventoryService(itemCatalog, configuration, loggerFactory.CreateLogger<InventoryService.InventoryService>(), logLanguage);
            inventories.ForEach(k => inventoryService[k.ItemInstanceId] =
                InventoryItemInstance.Create(itemProvider.Convert(itemInstances.First(s => s.Id == k.ItemInstanceId)),
                    characterId, k));

            var world = mapInstance.EcsWorld;
            var playerEntity = world.CreatePlayer(new PlayerSpawn(
                character,
                session.Account,
                script,
                configuration.Value.ServerId,
                mapInstance.MapInstanceId,
                character.MapX,
                character.MapY,
                (int)hpService.GetHp(character.Class, character.Level),
                (int)mpService.GetMp(character.Class, character.Level),
                speedService.GetSpeed(character.Class)));

            var group = new Group(GroupType.Group, sessionGroupFactory);
            world.AddComponent(playerEntity, new PlayerNetworkComponent(session, session.Channel));
            world.AddComponent(playerEntity, new PlayerContextComponent(mapInstance, group, null));
            world.AddComponent(playerEntity, new PlayerInventoryComponent(
                inventoryService,
                new ConcurrentDictionary<short, CharacterSkill>(),
                new ConcurrentDictionary<Guid, CharacterQuest>(),
                new List<QuicklistEntryDto>(),
                new List<StaticBonusDto>(),
                new List<TitleDto>(),
                new List<RespawnDto>()));
            world.AddComponent(playerEntity, new PlayerSocialComponent(
                new ConcurrentDictionary<long, long>(),
                null,
                null,
                new ConcurrentDictionary<long, long>(),
                new ConcurrentDictionary<long, long>(),
                new ConcurrentDictionary<long, Instant>(),
                null,
                new ConcurrentDictionary<(InviteType Type, long TargetId), Instant>(),
                new ConcurrentDictionary<long, long>(),
                new ConcurrentDictionary<long, long>()));
            world.AddComponent(playerEntity, new PlayerMatesComponent(new ConcurrentDictionary<long, Mate>()));
            session.SetPlayerEntity(playerEntity, world);

            var player = session.Character;
            group.JoinGroup(player);

            vitalityService.Refresh(player);
            if (player.Hp > player.MaxHp)
            {
                player.Hp = player.MaxHp;
            }

            if (player.Mp > player.MaxMp)
            {
                player.Mp = player.MaxMp;
            }

            LoadQuests(player);
            player.QuicklistEntries = quickListEntriesDao
                .Where(s => s.CharacterId == characterId)?.ToList() ?? new List<QuicklistEntryDto>();
            player.StaticBonusList = staticBonusDao
                .Where(s => s.CharacterId == characterId)?.ToList() ?? new List<StaticBonusDto>();
            player.Titles = titleDao
                .Where(s => s.CharacterId == characterId)?.ToList() ?? new List<TitleDto>();
            player.Respawns = respawnDao
                .Where(s => s.CharacterId == characterId)?.ToList() ?? new List<RespawnDto>();
            player.CompletedTimeSpaces = completedTimeSpaceDao
                .Where(s => s.CharacterId == characterId)?.Select(s => s.TimeSpaceId).ToImmutableHashSet();
            staticBuffService.Load(player);

            foreach (var mate in await mateService.LoadAsync(characterId))
            {
                player.Mates[mate.MateTransportId] = mate;
            }

            player.Family = await familyService.GetFamilyAsync(characterId);
            return player;
        }

        private void LoadQuests(PlayerComponentBundle player)
        {
            var characterId = player.CharacterId;
            var daoQuests = (characterQuestDao
                .Where(s => s.CharacterId == characterId) ?? new List<CharacterQuestDto>()).ToList();
            var daoQuestIds = daoQuests.Select(q => q.Id).ToHashSet();
            var daoObjectives = characterQuestObjectiveDao
                .Where(o => daoQuestIds.Contains(o.CharacterQuestId))?.ToList()
                ?? new List<CharacterQuestObjectiveDto>();
            player.Quests = new ConcurrentDictionary<Guid, CharacterQuest>(daoQuests.ToDictionary(x => x.Id, x =>
                {
                    var quest = (questCatalog.GetQuest(x.QuestId)
                        ?? throw new InvalidOperationException(logLanguage[LogLanguageKey.QUEST_NOT_FOUND]))
                        .Adapt<Quest>();
                    quest.QuestObjectives = questCatalog.GetObjectives(x.QuestId).ToList();
                    var charquest = new CharacterQuest { Quest = quest };
                    x.Adapt(charquest);
                    foreach (var progress in daoObjectives.Where(o => o.CharacterQuestId == charquest.Id))
                    {
                        charquest.ObjectiveProgress[progress.QuestObjectiveId] = progress.Count;
                    }
                    return charquest;
                }));
            var slotted = new List<CharacterQuest>();
            foreach (var open in player.Quests.Values.Where(q => q.CompletedOn == null).OrderBy(q => q.QuestId))
            {
                open.Slot = CharacterQuest.FreeSlot(slotted, open.Quest.IsSecondary);
                slotted.Add(open);
            }
        }
    }
}
