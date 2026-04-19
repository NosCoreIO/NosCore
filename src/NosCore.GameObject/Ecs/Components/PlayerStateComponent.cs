using NodaTime;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.ReputationService;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Networking;
using NosCore.Shared.I18N;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;

namespace NosCore.GameObject.Ecs.Components;

public record struct PlayerStateComponent(
    CharacterDto CharacterDto,
    AccountDto Account,
    IInventoryService InventoryService,
    IItemGenerationService ItemProvider,
    MapInstance MapInstance,
    Group? Group,
    Shop? Shop,
    ScriptDto? Script,
    ConcurrentDictionary<short, CharacterSkill> Skills,
    ConcurrentDictionary<Guid, CharacterQuest> Quests,
    List<QuicklistEntryDto> QuicklistEntries,
    List<StaticBonusDto> StaticBonusList,
    List<TitleDto> Titles,
    ConcurrentDictionary<long, long> GroupRequestCharacterIds,
    Dictionary<Type, Subject<RequestData>> Requests,
    bool IsChangingMapInstance,
    bool IsDisconnecting,
    bool InShop,
    bool InExchange,
    bool CanFight,
    Instant LastPortal,
    Instant LastSp,
    Instant? LastGroupRequest,
    short SpCooldown,
    byte VehicleSpeed,
    SemaphoreSlim HitSemaphore,
    IChannel? Channel,
    IPacketSender? Sender,
    IReputationService ReputationService,
    IDignityService DignityService,
    IGameLanguageLocalizer GameLanguageLocalizer,
    ConcurrentDictionary<IAliveEntity, int> HitList
);
