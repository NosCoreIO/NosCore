
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NodaTime;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.SpExperienceService;
using NosCore.Algorithm.SkillPointService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.I18N;
using NosCore.Core.Observability;
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
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Map;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Services.MapChangeService
{
    public class MapChangeService(IExperienceService experienceService, IJobExperienceService jobExperienceService,
            IHeroExperienceService heroExperienceService, ISpExperienceService spExperienceService, ISkillPointService skillPointService, IMapInstanceAccessorService mapInstanceAccessorService,
            IClock clock,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IMinilandService minilandProvider, ILogger<MapChangeService> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguageLocalizer, IGameLanguageLocalizer gameLanguageLocalizer,
            ISessionRegistry sessionRegistry, IGameEventDispatcher gameEvents,
            ScriptedInstanceService.IScriptedInstanceService scriptedInstanceService,
            BattleService.IBattleStatsProvider battleStatsProvider,
            FactionWarService.IFactionWarService factionWarService,
            ChannelService.IChannelService channelService, IRandomProvider random)
        : IMapChangeService
    {
        public async Task ChangeMapAsync(ClientSession session, short? mapId = null, short? mapX = null, short? mapY = null)
        {
            if (!session.HasPlayerEntity)
            {
                return;
            }

            var mapInstance = mapId != null
                ? mapInstanceAccessorService.GetBaseMapById((short)mapId)
                : session.Character.MapInstance;
            if (mapInstance == null)
            {
                logger.LogError(
                    logLanguageLocalizer[LogLanguageKey.MAP_DONT_EXIST, session.Account.Language]);
                return;
            }

            if (await channelService.TryMoveToHostAsync(session, mapInstance,
                    mapX ?? session.Character.MapX, mapY ?? session.Character.MapY))
            {
                return;
            }

            await ChangeMapInstanceAsync(session, mapInstance.MapInstanceId, mapX, mapY);
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
                        NosCoreMetrics.Failures.Add(1, new KeyValuePair<string, object?>("boundary", "map_change"));
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

                var fairy = character.InventoryService.LoadBySlotAndType(EquipmentType.Fairy, NoscorePocketType.Wear)?.ItemInstance as WearableInstance;
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
                await session.SendPacketAsync(character.GenerateLev(experienceService, jobExperienceService, heroExperienceService, spExperienceService, skillPointService));
                await session.SendPacketAsync(character.GenerateStat());
                await session.SendPacketAsync(character.GenerateAt(newMapInstance.Map.MapId, newMapInstance.Map.Music));
                await session.SendPacketAsync(character.GenerateCond());
                await session.SendPacketAsync(newMapInstance.GenerateCMap(true));
                await session.SendPacketAsync(character.GenerateSc(battleStatsProvider));
                await session.SendPacketAsync(character.GeneratePairy(fairy));
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

                // A player leaving destroys their entity on the map's executor, so the others are read there.
                var others = await newMapInstance.Executor.RunAsync(() => Task.FromResult(sessionRegistry
                    .GetSessions(s => s.HasPlayerEntity && s != session && s.Character.MapInstanceId == mapInstanceId)
                    .SelectMany(s => DescribeTo(s.Character))
                    .ToList()));
                await session.SendPacketsAsync(others);

                IEnumerable<IPacket> DescribeTo(PlayerComponentBundle otherCharacter)
                {
                    var prefix = otherCharacter.Authority == AuthorityType.Moderator
                        ? $"[{gameLanguageLocalizer[LanguageKey.SUPPORT, otherCharacter.AccountLanguage]}]"
                        : string.Empty;
                    yield return otherCharacter.GenerateIn(prefix);
                    yield return otherCharacter.GenerateGidx(gameLanguageLocalizer, accountLanguage);

                    if (!otherCharacter.Invisible)
                    {
                        foreach (var mate in otherCharacter.Mates.Values.Where(m => m.Entity.HasValue))
                        {
                            yield return mate.GenerateIn(accountLanguage, mate.Entity.GetValueOrDefault().PositionX,
                                mate.Entity.GetValueOrDefault().PositionY);
                        }
                    }

                    if (otherCharacter.Shop != null)
                    {
                        yield return otherCharacter.GeneratePFlag();
                        yield return otherCharacter.GenerateShop(session.Account.Language);
                    }
                }

                await newMapInstance.SendPacketAsync(titInfoPacket);
                newMapInstance.IsSleeping = false;

                // On the map's executor no tick can respawn or move anything between the snapshot and
                // the registration, so what the player is not shown here reaches them as a broadcast.
                await newMapInstance.Executor.RunAsync(async () =>
                {
                    if (channelId != null)
                    {
                        newMapInstance.Sessions.Add(session.ConnectedChannel);
                    }

                    await session.SendPacketsAsync(newMapInstance.GetMapItems(accountLanguage));
                });

                if (channelId != null)
                {
                    // A hidden player still arrives, flagged so the others do not draw them until they are revealed.
                    var prefix = authority == AuthorityType.Moderator
                        ? $"[{gameLanguageLocalizer[LanguageKey.SUPPORT, accountLanguage]}]"
                        : string.Empty;
                    await newMapInstance.SendPacketAsync(character.GenerateIn(prefix), new EveryoneBut(channelId));
                    if (!invisible)
                    {
                        var watchers = await newMapInstance.Executor.RunAsync(() => Task.FromResult(sessionRegistry
                            .GetSessions(s => s.HasPlayerEntity && s != session && s.Character.MapInstanceId == mapInstanceId)
                            .Select(watcher => (watcher, watcher.Character.AccountLanguage))
                            .ToList()));
                        await Task.WhenAll(watchers.Select(w =>
                            w.watcher.SendPacketAsync(character.GenerateGidx(gameLanguageLocalizer, w.AccountLanguage))));
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
                        var (x, y) = MatePlacement.MinilandSpot(mate, newMapInstance.Map, random);
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

                await gameEvents.RaiseAsync(new Messaging.Events.MapInstanceEnteredEvent(session, newMapInstance, currentMapInstance));
                await gameEvents.RaiseAsync(new MovementConditionChangedEvent(character));
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
