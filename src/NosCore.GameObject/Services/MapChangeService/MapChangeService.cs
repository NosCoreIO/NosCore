
using NodaTime;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.SkillPointService;
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
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Map;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NosCore.GameObject.Messaging.Events;

namespace NosCore.GameObject.Services.MapChangeService
{
    public class MapChangeService(IExperienceService experienceService, IJobExperienceService jobExperienceService,
            IHeroExperienceService heroExperienceService, ISkillPointService skillPointService, IMapInstanceAccessorService mapInstanceAccessorService,
            IClock clock,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IMinilandService minilandProvider, ILogger<MapChangeService> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguageLocalizer, IGameLanguageLocalizer gameLanguageLocalizer,
            ISessionRegistry sessionRegistry, Wolverine.IMessageBus messageBus,
            ScriptedInstanceService.IScriptedInstanceService scriptedInstanceService,
            BattleService.IBattleStatsProvider battleStatsProvider,
            FactionWarService.IFactionWarService factionWarService)
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
                    logger.LogError(
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
            // Work running on a map must not wait for a session; the change runs once that work is over.
            if (MapExecutor.IsInsideAny)
            {
                RunDetached(() => ChangeMapInstanceAsync(session, mapInstanceId, mapX, mapY));
                return;
            }

            using (await session.AcquirePacketLockUnlessHeldAsync())
            {
                if (!session.TryBeginMapChange())
                {
                    return;
                }

                try
                {
                    await ChangeMapInstanceClaimedAsync(session, mapInstanceId, mapX, mapY);
                }
                finally
                {
                    session.EndMapChange();
                }
            }
        }

        private void RunDetached(Func<Task> work)
        {
            using (ExecutionContext.SuppressFlow())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await work();
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, logLanguage[LogLanguageKey.ERROR_CHANGE_MAP]);
                    }
                });
            }
        }

        private async Task ChangeMapInstanceClaimedAsync(ClientSession session, Guid mapInstanceId, int? mapX, int? mapY)
        {
            if (!session.HasPlayerEntity)
            {
                return;
            }

            var character = session.Character;
            if (character.MapInstance == null)
            {
                return;
            }

            character.IsChangingMapInstance = true;
            try
            {
                if (character.Shop != null)
                {
                    await character.CloseShopAsync();
                }

                var currentMapInstance = character.MapInstance;
                var newMapInstance = mapInstanceAccessorService.GetMapInstance(mapInstanceId);
                if (newMapInstance == null || newMapInstance.Executor.IsClosed)
                {
                    logger.LogError(logLanguageLocalizer[LogLanguageKey.MAP_DONT_EXIST, session.Account.Language]);
                    return;
                }

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

                if (session.Channel?.Id != null)
                {
                    currentMapInstance.Sessions.Remove(session.Channel);
                }
                await session.SendPacketAsync(currentMapInstance.GenerateCMap(false));
                currentMapInstance.LastUnregister = clock.GetCurrentInstant();
                await currentMapInstance.SendPacketAsync(character.GenerateOut(), new EveryoneBut(session.ConnectedChannel.Id));
                foreach (var mate in character.Mates.Values)
                {
                    await mate.DespawnAsync(currentMapInstance);
                }

                // Holding both maps keeps their own work away from the player while they move;
                // holding both worlds keeps everything else away for the copy itself.
                var leaving = currentMapInstance.Executor.IsClosed ? newMapInstance.Executor : currentMapInstance.Executor;
                var moved = false;
                await MapExecutor.RunExclusiveAsync(leaving, newMapInstance.Executor, () =>
                {
                    moved = MovePlayer(session, currentMapInstance, newMapInstance, mapX, mapY);
                    return Task.CompletedTask;
                });
                if (!moved)
                {
                    return;
                }

                await session.SendPacketAsync(new MapOutPacket());
                if (currentMapInstance.Sessions.Count == 0)
                {
                    currentMapInstance.IsSleeping = true;
                }

                var abandonedRun = scriptedInstanceService.GetRun(currentMapInstance.MapInstanceId);

                character = session.Character;

                if (abandonedRun != null && scriptedInstanceService.GetRun(mapInstanceId) != abandonedRun)
                {
                    await scriptedInstanceService.DisposeIfEmptyAsync(abandonedRun);
                }

                character.Group.LeaveGroup(character);
                character.Group.JoinGroup(character);

                var fairy = character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance as WearableInstance;
                var group = character.Group;
                var channelId = character.Channel?.Id;
                var titInfoPacket = character.GenerateTitInfo();
                var accountLanguage = character.AccountLanguage;
                var invisible = character.Invisible;
                var authority = character.Authority;

                await session.SendPacketAsync(character.GenerateCInfo(gameLanguageLocalizer));
                await session.SendPacketAsync(character.GenerateCMode());
                await session.SendPacketAsync(character.GenerateEq());
                await session.SendPacketAsync(character.GenerateEquipment());
                await session.SendPacketAsync(character.GenerateLev(experienceService, jobExperienceService, heroExperienceService, skillPointService));
                await session.SendPacketAsync(character.GenerateStat());
                await session.SendPacketAsync(character.GenerateAt(newMapInstance.Map.MapId, newMapInstance.Map.Music));
                await session.SendPacketAsync(character.GenerateCond());
                await session.SendPacketAsync(newMapInstance.GenerateCMap(true));
                await session.SendPacketAsync(character.GenerateSc(battleStatsProvider));
                await session.SendPacketAsync(character.GeneratePairy(fairy));
                await session.SendPacketsAsync(newMapInstance.GetMapItems(accountLanguage));
                await session.SendPacketsAsync(newMapInstance.MapDesignObjects.Values.Select(mp => mp.GenerateEffect()));

                if (newMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    await session.SendPacketsAsync(scriptedInstanceService.GenerateWp(newMapInstance.Map.MapId));
                    await session.SendPacketsAsync(scriptedInstanceService.GenerateRaidGates(newMapInstance.Map.MapId));
                }

                if (newMapInstance.IsAct4)
                {
                    await session.SendPacketAsync(factionWarService.GenerateFc(character));
                    if (factionWarService.GenerateRaidGate(character) is { } raidGate)
                    {
                        await session.SendPacketAsync(raidGate);
                    }
                }

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
                    await session.SendPacketAsync(otherCharacter.GenerateGidx(gameLanguageLocalizer, accountLanguage));

                    if (!otherCharacter.Invisible)
                    {
                        await session.SendPacketsAsync(otherCharacter.Mates.Values
                            .Where(m => m.Entity.HasValue)
                            .Select(m => m.GenerateIn(accountLanguage, m.Entity.GetValueOrDefault().PositionX,
                                m.Entity.GetValueOrDefault().PositionY)));
                    }

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
                    newMapInstance.Sessions.Add(session.ConnectedChannel);
                    if (!invisible)
                    {
                        var prefix = authority == AuthorityType.Moderator
                            ? $"[{gameLanguageLocalizer[LanguageKey.SUPPORT, accountLanguage]}]"
                            : string.Empty;
                        await newMapInstance.SendPacketAsync(character.GenerateIn(prefix), new EveryoneBut(channelId));

                        var watchers = sessionRegistry.GetSessions(s =>
                            s.HasPlayerEntity && s != session && s.Character.MapInstanceId == mapInstanceId);
                        await Task.WhenAll(watchers.Select(watcher =>
                            watcher.SendPacketAsync(character.GenerateGidx(gameLanguageLocalizer,
                                watcher.Character.AccountLanguage))));
                    }
                }

                var teamMates = character.Mates.Values.Where(s => s.IsTeamMember && s.RevivesAt == null).ToList();
                foreach (var (mate, x, y) in MatePlacement.Arrange(character.PositionX, character.PositionY,
                    newMapInstance.Map, character.IsVehicled ? [] : teamMates))
                {
                    await mate.SpawnAsync(character, newMapInstance, x, y);
                }

                if (minilandProvider.GetMinilandFromMapInstanceId(mapInstanceId)?.OwnerId == characterId)
                {
                    foreach (var mate in character.Mates.Values.Where(s => !s.IsTeamMember))
                    {
                        var (x, y) = MatePlacement.MinilandSpot(mate, newMapInstance.Map);
                        await mate.SpawnAsync(character, newMapInstance, x, y);
                    }
                }

                await session.SendPacketAsync(character.GeneratePinit());
                if (group is { IsEmpty: false })
                {
                    await session.SendPacketsAsync(group.GeneratePst());
                }

                await session.SendPacketsAsync(teamMates.Select(s => s.GenerateCond()));
                await session.SendPacketsAsync(teamMates.Select(s => s.GeneratePst()));

                await messageBus.PublishAsync(new Messaging.Events.MapInstanceEnteredEvent(session, newMapInstance, currentMapInstance));
                await messageBus.PublishAsync(new MovementConditionChangedEvent(character));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, logLanguage[LogLanguageKey.ERROR_CHANGE_MAP]);
            }
            finally
            {
                if (session.HasPlayerEntity)
                {
                    session.Character.IsChangingMapInstance = false;
                }
            }
        }

        private static bool MovePlayer(ClientSession session, MapInstance from, MapInstance to, int? mapX, int? mapY)
        {
            var moved = false;
            MapWorld.Exclusive(from.EcsWorld, to.EcsWorld, () =>
            {
                if (!session.HasPlayerEntity)
                {
                    return;
                }

                var oldWorld = session.Character.World;
                var oldEntity = session.Character.Entity;
                if (oldWorld != from.EcsWorld)
                {
                    return;
                }

                var position = oldWorld.TryGetComponent<PositionComponent>(oldEntity) ?? default;
                var visual = oldWorld.TryGetComponent<VisualComponent>(oldEntity) ?? default;
                var context = oldWorld.TryGetComponent<PlayerContextComponent>(oldEntity) ?? default;
                var playerEntity = to.EcsWorld.ClonePlayer(
                    oldWorld.TryGetComponent<EntityIdentityComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<HealthComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<ManaComponent>(oldEntity) ?? default,
                    position with
                    {
                        MapInstanceId = to.MapInstanceId,
                        PositionX = mapX != null ? (short)mapX : position.PositionX,
                        PositionY = mapY != null ? (short)mapY : position.PositionY
                    },
                    visual with { IsSitting = false },
                    oldWorld.TryGetComponent<AppearanceComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<ExperienceComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<GoldComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<ReputationComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<SpComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<NameComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<CombatComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<BuffStateComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<PlayerComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<PlayerFlagsComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<TimingComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<SpeedComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<PlayerStateComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<PlayerNetworkComponent>(oldEntity) ?? default,
                    context with { MapInstance = to },
                    oldWorld.TryGetComponent<PlayerInventoryComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<PlayerSocialComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<RequestsComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<PlayerMatesComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<DigestionComponent>(oldEntity) ?? new DigestionComponent(new DigestionState()),
                    oldWorld.TryGetComponent<HitListComponent>(oldEntity) ?? new HitListComponent(new()),
                    oldWorld.TryGetComponent<BaseMapPositionComponent>(oldEntity) ?? default,
                    oldWorld.TryGetComponent<SkillResetComponent>(oldEntity) ?? new SkillResetComponent(new()),
                    oldWorld.TryGetComponent<FactionComponent>(oldEntity) ?? default);

                session.SetPlayerEntity(playerEntity, to.EcsWorld);
                oldWorld.Forward(oldEntity, session);
                oldWorld.DestroyEntity(oldEntity);
                moved = true;
            });
            return moved;
        }

        public async Task ChangeMapByCharacterIdAsync(long characterId, short mapId, short mapX, short mapY)
        {
            var sender = sessionRegistry.GetSenderByCharacterId(characterId);
            if (sender is ClientSession session)
            {
                await ChangeMapAsync(session, mapId, mapX, mapY);
            }
        }
    }
}
