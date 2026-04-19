
using NodaTime;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Map;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapChangeService
{
    public class MapChangeService(IExperienceService experienceService, IJobExperienceService jobExperienceService,
            IHeroExperienceService heroExperienceService, IMapInstanceAccessorService mapInstanceAccessorService,
            IClock clock,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IMinilandService minilandProvider, ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguageLocalizer, IGameLanguageLocalizer gameLanguageLocalizer,
            ISessionRegistry sessionRegistry)
        : IMapChangeService
    {
        public async Task ChangeMapAsync(ClientSession session, short? mapId = null, short? mapX = null, short? mapY = null)
        {
            if (!session.HasPlayerEntity)
            {
                return;
            }

            Guid targetMapInstanceId;
            if (mapId != null)
            {
                var mapInstance = mapInstanceAccessorService.GetBaseMapById((short)mapId);

                if (mapInstance == null)
                {
                    logger.Error(
                        logLanguageLocalizer[LogLanguageKey.MAP_DONT_EXIST, session.Account.Language]);
                    return;
                }

                targetMapInstanceId = mapInstance.MapInstanceId;
            }
            else
            {
                targetMapInstanceId = session.Character.MapInstanceId;
            }

            await ChangeMapInstanceAsync(session, targetMapInstanceId, mapX, mapY);
        }

        public async Task ChangeMapInstanceAsync(ClientSession session, Guid mapInstanceId, int? mapX = null, int? mapY = null)
        {
            if (!session.HasPlayerEntity)
            {
                return;
            }

            var character = session.Character;
            if (character.MapInstance == null || character.IsChangingMapInstance)
            {
                return;
            }

            try
            {
                character.IsChangingMapInstance = true;

                var currentMapInstance = character.MapInstance;
                var newMapInstance = mapInstanceAccessorService.GetMapInstance(mapInstanceId)!;
                var characterId = character.CharacterId;

                if (newMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    character.MapId = newMapInstance.Map.MapId;
                    if ((mapX != null) && (mapY != null))
                    {
                        character.MapX = (short)mapX;
                        character.MapY = (short)mapY;
                    }
                }

                var oldWorld = character.World;
                var oldEntity = character.Entity;

                var identity = oldWorld.TryGetComponent<EntityIdentityComponent>(oldEntity) ?? default;
                var health = oldWorld.TryGetComponent<HealthComponent>(oldEntity) ?? default;
                var mana = oldWorld.TryGetComponent<ManaComponent>(oldEntity) ?? default;
                var position = oldWorld.TryGetComponent<PositionComponent>(oldEntity) ?? default;
                var visual = oldWorld.TryGetComponent<VisualComponent>(oldEntity) ?? default;
                var appearance = oldWorld.TryGetComponent<AppearanceComponent>(oldEntity) ?? default;
                var experience = oldWorld.TryGetComponent<ExperienceComponent>(oldEntity) ?? default;
                var goldComp = oldWorld.TryGetComponent<GoldComponent>(oldEntity) ?? default;
                var reputation = oldWorld.TryGetComponent<ReputationComponent>(oldEntity) ?? default;
                var sp = oldWorld.TryGetComponent<SpComponent>(oldEntity) ?? default;
                var nameComp = oldWorld.TryGetComponent<NameComponent>(oldEntity) ?? default;
                var combat = oldWorld.TryGetComponent<CombatComponent>(oldEntity) ?? default;
                var playerComp = oldWorld.TryGetComponent<PlayerComponent>(oldEntity) ?? default;
                var playerFlags = oldWorld.TryGetComponent<PlayerFlagsComponent>(oldEntity) ?? default;
                var timing = oldWorld.TryGetComponent<TimingComponent>(oldEntity) ?? default;
                var speedComp = oldWorld.TryGetComponent<SpeedComponent>(oldEntity) ?? default;
                var state = oldWorld.TryGetComponent<PlayerStateComponent>(oldEntity) ?? default;

                if (session.Channel?.Id != null)
                {
                    currentMapInstance.Sessions.Remove(session.Channel);
                }
                await session.SendPacketAsync(currentMapInstance.GenerateCMap(false));
                currentMapInstance.LastUnregister = clock.GetCurrentInstant();
                await LeaveMapAsync(session);
                if (currentMapInstance.Sessions.Count == 0)
                {
                    currentMapInstance.IsSleeping = true;
                }

                var playerEntity = newMapInstance.EcsWorld.ClonePlayer(
                    identity,
                    health,
                    mana,
                    position with
                    {
                        MapInstanceId = newMapInstance.MapInstanceId,
                        PositionX = mapX != null ? (short)mapX : position.PositionX,
                        PositionY = mapY != null ? (short)mapY : position.PositionY
                    },
                    visual with { IsSitting = false },
                    appearance,
                    experience,
                    goldComp,
                    reputation,
                    sp,
                    nameComp,
                    combat,
                    playerComp,
                    playerFlags,
                    timing,
                    speedComp,
                    state with { MapInstance = newMapInstance });

                session.SetPlayerEntity(playerEntity, newMapInstance.EcsWorld);
                character = session.Character;

                character.Group?.LeaveGroup(character);
                character.Group?.JoinGroup(character);

                var fairy = character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance as WearableInstance;
                var group = character.Group;
                var channelId = character.Channel?.Id;
                var titInfoPacket = character.GenerateTitInfo();
                var accountLanguage = character.AccountLanguage;
                var invisible = character.Invisible;
                var authority = character.Authority;

                await session.SendPacketAsync(character.GenerateCInfo());
                await session.SendPacketAsync(character.GenerateCMode());
                await session.SendPacketAsync(character.GenerateEq());
                await session.SendPacketAsync(character.GenerateEquipment());
                await session.SendPacketAsync(character.GenerateLev(experienceService, jobExperienceService, heroExperienceService));
                await session.SendPacketAsync(character.GenerateStat());
                await session.SendPacketAsync(character.GenerateAt(newMapInstance.Map.MapId, newMapInstance.Map.Music));
                await session.SendPacketAsync(character.GenerateCond());
                await session.SendPacketAsync(newMapInstance.GenerateCMap(true));
                await session.SendPacketAsync(character.GeneratePairy(fairy));
                await session.SendPacketsAsync(newMapInstance.GetMapItems(accountLanguage));
                await session.SendPacketsAsync(newMapInstance.MapDesignObjects.Values.Select(mp => mp.GenerateEffect()));

                var minilandPortals = minilandProvider
                    .GetMinilandPortals(characterId)
                    .Where(s => s.SourceMapInstanceId == mapInstanceId)
                    .ToList();
                if (minilandPortals.Count > 0)
                {
                    await session.SendPacketsAsync(minilandPortals.Select(s => s.GenerateGp()));
                }

                if (group != null)
                {
                    await session.SendPacketAsync(group.GeneratePinit());
                    if (!group.IsEmpty)
                    {
                        await session.SendPacketsAsync(group.GeneratePst());
                    }

                    if (group.Type == GroupType.Group && group.Count > 1)
                    {
                        await newMapInstance.SendPacketAsync(group.GeneratePidx(character));
                    }
                }

                var mapSessions = sessionRegistry.GetSessions(s =>
                    s.HasPlayerEntity && s != session && s.Character.MapInstanceId == mapInstanceId);
                await Task.WhenAll(mapSessions.Select(async s =>
                {
                    var otherCharacter = s.Character;
                    var prefix = otherCharacter.Authority == AuthorityType.Moderator
                        ? $"[{gameLanguageLocalizer[LanguageKey.SUPPORT, otherCharacter.AccountLanguage]}]"
                        : string.Empty;
                    await session.SendPacketAsync(otherCharacter.GenerateIn(prefix));

                    var shop = otherCharacter.Shop;
                    if (shop != null)
                    {
                        await session.SendPacketAsync(otherCharacter.GeneratePFlag());
                        await session.SendPacketAsync(otherCharacter.GenerateShop(session.Account.Language));
                    }
                }));

                await newMapInstance.SendPacketAsync(titInfoPacket);
                newMapInstance.IsSleeping = false;

                if (channelId != null)
                {
                    if (!invisible)
                    {
                        var prefix = authority == AuthorityType.Moderator
                            ? $"[{gameLanguageLocalizer[LanguageKey.SUPPORT, accountLanguage]}]"
                            : string.Empty;
                        await newMapInstance.SendPacketAsync(character.GenerateIn(prefix), new EveryoneBut(channelId));
                    }

                    newMapInstance.Sessions.Add(session.Channel!);
                }

                newMapInstance.Requests[typeof(IMapInstanceEntranceEventHandler)]?
                    .OnNext(new RequestData<MapInstance>(session, newMapInstance));

                character.IsChangingMapInstance = false;
            }
            catch (Exception ex)
            {
                logger.Warning(ex, logLanguage[LogLanguageKey.ERROR_CHANGE_MAP]);
                if (session.HasPlayerEntity)
                {
                    session.Character.IsChangingMapInstance = false;
                }
            }
        }

        public async Task ChangeMapByCharacterIdAsync(long characterId, short mapId, short mapX, short mapY)
        {
            var sender = sessionRegistry.GetSenderByCharacterId(characterId);
            if (sender is ClientSession session)
            {
                await ChangeMapAsync(session, mapId, mapX, mapY);
            }
        }

        private async Task LeaveMapAsync(ClientSession session)
        {
            var character = session.Character;
            var outPacket = character.GenerateOut();
            var mapInstance = character.MapInstance;
            session.ClearPlayerEntity();
            await session.SendPacketAsync(new MapOutPacket());
            await mapInstance.SendPacketAsync(outPacket, new EveryoneBut(session.Channel!.Id));
        }

    }
}
