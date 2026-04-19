//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Algorithm.ReputationService;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.GuriRunnerService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.NRunService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Core.I18N;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class SelectPacketHandler(IDao<CharacterDto, long> characterDao, ILogger logger,
            IItemGenerationService itemProvider,
            IMapInstanceAccessorService mapInstanceAccessorService, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IDao<InventoryItemInstanceDto, Guid> inventoryItemInstanceDao, IDao<StaticBonusDto, long> staticBonusDao,
            IDao<QuicklistEntryDto, Guid> quickListEntriesDao, IDao<TitleDto, Guid> titleDao,
            IDao<CharacterQuestDto, Guid> characterQuestDao,
            IDao<ScriptDto, Guid> scriptDao, List<QuestDto> quests, List<QuestObjectiveDto> questObjectives,
            IOptions<WorldConfiguration> configuration, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            IPubSubHub pubSubHub, IReputationService reputationService, IDignityService dignityService, IClock clock,
            List<ItemDto> items, IHpService hpService, IMpService mpService, ISessionGroupFactory sessionGroupFactory,
            ICharacterInitializationService characterInitializationService, IGameLanguageLocalizer gameLanguageLocalizer)
        : PacketHandler<SelectPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SelectPacket packet, ClientSession clientSession)
        {
            try
            {
                var characterDto = await
                    characterDao.FirstOrDefaultAsync(s =>
                        (s.AccountId == clientSession.Account.AccountId) && (s.Slot == packet.Slot)
                        && (s.State == CharacterState.Active) && s.ServerId == configuration.Value.ServerId);
                if (characterDto == null)
                {
                    logger.Error(logLanguage[LogLanguageKey.CHARACTER_SLOT_EMPTY], new
                    {
                        clientSession.Account.AccountId,
                        packet.Slot
                    });
                    return;
                }

                var characterId = characterDto.CharacterId;
                var characterName = characterDto.Name ?? string.Empty;

                await pubSubHub.SubscribeAsync(new Subscriber
                {
                    Id = clientSession.SessionId,
                    Name = clientSession.Account.Name,
                    Language = clientSession.Account.Language,
                    ConnectedCharacter = new Data.WebApi.Character
                    {
                        Name = characterName,
                        Id = characterId,
                        FriendRequestBlocked = characterDto.FriendRequestBlocked
                    }
                });

                var mapInstance = mapInstanceAccessorService.GetBaseMapById(characterDto.MapId)!;
                var script = characterDto.CurrentScriptId != null
                    ? await scriptDao.FirstOrDefaultAsync(s => s.Id == characterDto.CurrentScriptId)
                    : null;

                var inventories = inventoryItemInstanceDao
                    .Where(s => s.CharacterId == characterId)
                    ?.ToList() ?? new List<InventoryItemInstanceDto>();
                var ids = inventories.Select(o => o.ItemInstanceId).ToList();
                var itemInstances = itemInstanceDao.Where(s => ids.Contains(s!.Id))?.ToList() ?? new List<IItemInstanceDto?>();

                var inventoryService = new InventoryService(items, configuration, logger);
                inventories.ForEach(k => inventoryService[k.ItemInstanceId] =
                    InventoryItemInstance.Create(itemProvider.Convert(itemInstances.First(s => s!.Id == k.ItemInstanceId)!),
                        characterId, k));

                var maxHp = (int)hpService.GetHp(characterDto.Class, characterDto.Level);
                var maxMp = (int)mpService.GetMp(characterDto.Class, characterDto.Level);
                var authority = clientSession.Account.Authority;

                var playerEntity = mapInstance.EcsWorld.CreatePlayer(
                    (int)characterId,
                    characterId,
                    clientSession.Account.AccountId,
                    characterName,
                    mapInstance.MapInstanceId,
                    characterDto.MapX,
                    characterDto.MapY,
                    2,
                    characterDto.Hp,
                    maxHp,
                    characterDto.Mp,
                    maxMp,
                    characterDto.Level,
                    characterDto.LevelXp,
                    characterDto.JobLevel,
                    characterDto.JobLevelXp,
                    characterDto.HeroLevel,
                    characterDto.HeroXp,
                    characterDto.Gold,
                    characterDto.Reput,
                    (short)characterDto.Dignity,
                    (short)characterDto.Compliment,
                    characterDto.Gender,
                    characterDto.HairStyle,
                    characterDto.HairColor,
                    characterDto.Class,
                    0,
                    10,
                    authority,
                    authority >= NosCore.Shared.Enumerations.AuthorityType.GameMaster,
                    configuration.Value.ServerId);

                var now = clock.GetCurrentInstant();
                var group = new NosCore.GameObject.Services.GroupService.Group(GroupType.Group, sessionGroupFactory);
                var playerStateComponent = new PlayerStateComponent(
                    characterDto,
                    clientSession.Account,
                    inventoryService,
                    itemProvider,
                    mapInstance,
                    group,
                    null,
                    script,
                    new ConcurrentDictionary<short, CharacterSkill>(),
                    new ConcurrentDictionary<Guid, CharacterQuest>(),
                    new List<QuicklistEntryDto>(),
                    new List<StaticBonusDto>(),
                    new List<TitleDto>(),
                    new ConcurrentDictionary<long, long>(),
                    new Dictionary<Type, Subject<RequestData>>
                    {
                        { typeof(IUseItemEventHandler), new Subject<RequestData>() },
                        { typeof(INrunEventHandler), new Subject<RequestData>() }
                    },
                    false,
                    false,
                    false,
                    false,
                    true,
                    now,
                    now,
                    null,
                    0,
                    0,
                    new SemaphoreSlim(1, 1),
                    clientSession.Channel,
                    clientSession,
                    reputationService,
                    dignityService,
                    gameLanguageLocalizer,
                    new ConcurrentDictionary<IAliveEntity, int>()
                );

                mapInstance.EcsWorld.AddComponent(playerEntity, playerStateComponent);
                clientSession.SetPlayerEntity(playerEntity, mapInstance.EcsWorld);

                var character = clientSession.Character;
                group.JoinGroup(character);

#pragma warning disable CS0618
                await clientSession.SendPacketsAsync(character.GenerateInv(logger, logLanguage));
#pragma warning restore CS0618
                await clientSession.SendPacketAsync(character.GenerateMlobjlst());

                if (character.Hp > character.MaxHp)
                {
                    character.Hp = character.MaxHp;
                }

                if (character.Mp > character.MaxMp)
                {
                    character.Mp = character.MaxMp;
                }

                var daoQuests = characterQuestDao
                    .Where(s => s.CharacterId == characterId) ?? new List<CharacterQuestDto>();
                character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>(daoQuests.ToDictionary(x => x.Id, x =>
                    {
                        var charquest = x.Adapt<CharacterQuest>();
                        charquest.Quest = quests.First(s => s.QuestId == charquest.QuestId).Adapt<GameObject.Services.QuestService.Quest>();
                        charquest.Quest.QuestObjectives =
                            questObjectives.Where(s => s.QuestId == charquest.QuestId).ToList();
                        return charquest;
                    }));
                character.QuicklistEntries = quickListEntriesDao
                    .Where(s => s.CharacterId == characterId)?.ToList() ?? new List<QuicklistEntryDto>();
                character.StaticBonusList = staticBonusDao
                    .Where(s => s.CharacterId == characterId)?.ToList() ?? new List<StaticBonusDto>();
                character.Titles = titleDao
                    .Where(s => s.CharacterId == characterId)?.ToList() ?? new List<TitleDto>();

                await characterInitializationService.InitializeAsync(character);

                await clientSession.SendPacketAsync(new OkPacket());
            }
            catch (Exception ex)
            {
                logger.Error(logLanguage[LogLanguageKey.CHARACTER_SELECTION_FAILED], ex, new
                {
                    clientSession.Account.AccountId,
                    packet.Slot
                });
            }
        }
    }
}
