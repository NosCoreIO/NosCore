//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.LandOfDeathService
{
    // Every family gets its own copy of the land, made when its first member goes in. The Flying Fire
    // Devil hunts the last player to arrive, and experience and drops are tripled while it is out.
    public class LandOfDeathService(
        List<NpcMonsterDto> npcMonsters,
        IDao<MapMonsterDto, int> mapMonsters,
        Lazy<IMapInstanceGeneratorService> mapInstanceGeneratorService,
        IMapInstanceRegistry mapInstanceRegistry,
        IMapChangeService mapChangeService,
        ISessionRegistry sessionRegistry,
        IAggroService aggroService,
        IItemCatalog itemCatalog,
        IClock clock,
        ILogger<LandOfDeathService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : ILandOfDeathService, ISingletonService
    {
        public const short MapId = 150;
        public const short EntranceX = 153;
        public const short EntranceY = 145;
        public const byte MinimumLevel = 55;
        public const short FlyingFireDevilVNum = 443;
        public const int HuntRate = 3;

        public static readonly TimeSpan Length = TimeSpan.FromMinutes(60);
        public static readonly TimeSpan HuntStart = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan HuntEvery = TimeSpan.FromMinutes(4);
        public static readonly TimeSpan HuntStay = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan ClockRefresh = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan ClosingWarning = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan ReviveTimeout = TimeSpan.FromSeconds(30);

        private const short DevilDefaultX = 154;
        private const short DevilDefaultY = 140;

        private readonly ConcurrentDictionary<long, MapInstance> _lands = new();
        private readonly ConcurrentDictionary<Guid, long> _lastArrival = new();
        private readonly ConcurrentDictionary<Guid, MonsterComponentBundle> _devils = new();
        private readonly SemaphoreSlim _landLock = new(1, 1);
        private int _running;
        private volatile bool _gateOpen;
        private volatile bool _huntActive;
        private TimeSpan _remaining;

        public bool IsRunning => Volatile.Read(ref _running) == 1;

        public bool IsGateOpen => _gateOpen;

        public static bool IsHuntStart(TimeSpan remaining)
        {
            var hunted = HuntStart - remaining;
            return hunted >= TimeSpan.Zero && remaining > TimeSpan.Zero && hunted.Ticks % HuntEvery.Ticks == 0;
        }

        public static bool IsHuntEnd(TimeSpan remaining)
        {
            var hunted = HuntStart - HuntStay - remaining;
            return hunted >= TimeSpan.Zero && hunted.Ticks % HuntEvery.Ticks == 0;
        }

        public bool TryStart()
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            {
                return false;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await RunAsync();
                }
                catch (Exception e)
                {
                    logger.LogError(e, logLanguage[LogLanguageKey.LAND_OF_DEATH_FAILED]);
                }
                finally
                {
                    _gateOpen = false;
                    _huntActive = false;
                    await CloseAllAsync();
                    Volatile.Write(ref _running, 0);
                }
            });
            return true;
        }

        public async Task<bool> CanEnterAsync(ClientSession session)
        {
            var character = session.Character;
            if (character.Family == null)
            {
                await character.SendPacketAsync(Message(Game18NConstString.NeedFamilyForLandOfDeath));
                return false;
            }

            if (!IsGateOpen)
            {
                await character.SendPacketAsync(Message(Game18NConstString.NotAuthorised));
                return false;
            }

            if (character.Level < MinimumLevel)
            {
                await character.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.TooLowLevelToEntry,
                    ArgumentType = 4,
                    Game18NArguments = { MinimumLevel },
                });
                return false;
            }

            return character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance && character.IsAlive;
        }

        public async Task EnterAsync(ClientSession session)
        {
            if (!await CanEnterAsync(session) || session.Character.Family is not { } family)
            {
                return;
            }

            var land = await GetOrCreateLandAsync(family.FamilyId);
            if (land == null)
            {
                return;
            }

            _lastArrival[land.MapInstanceId] = session.Character.CharacterId;
            await mapChangeService.ChangeMapInstanceAsync(session, land.MapInstanceId, EntranceX, EntranceY);
            await session.SendPacketAsync(Clock(_remaining));

            // A family arriving while the devil is out elsewhere gets its own devil straight away.
            await _landLock.WaitAsync();
            try
            {
                if (_huntActive && !_devils.ContainsKey(land.MapInstanceId))
                {
                    await land.Executor.RunAsync(() => StartHuntAsync(land));
                }
            }
            finally
            {
                _landLock.Release();
            }
        }

        public async Task AskReviveAsync(PlayerComponentBundle player)
        {
            await player.SendPacketAsync(player.GenerateStat());
            await player.SendPacketAsync(new DlgiPacket
            {
                YesPacket = new RevivalPacket { Type = RevivalType.InPlace },
                NoPacket = new RevivalPacket { Type = RevivalType.AtSavePoint },
                Question = Game18NConstString.AskWantToUse,
                ArgumentType = 2,
                Game18NArguments = new Game18NArguments(2) { itemCatalog.GetMaterialVNum(MaterialType.Saver), 1 },
            });

            var mapInstanceId = player.MapInstance?.MapInstanceId;
            var askedAt = clock.GetCurrentInstant();
            player.ReviveAskedAt = askedAt;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(ReviveTimeout);
                    if (player.ReviveAskedAt != askedAt || player.Hp > 0 || player.MapInstance?.MapInstanceId != mapInstanceId
                        || sessionRegistry.GetSessionByCharacterId(player.CharacterId) is not { } session)
                    {
                        return;
                    }

                    await ReturnToEntranceAsync(session);
                }
                catch (Exception e)
                {
                    logger.LogError(e, logLanguage[LogLanguageKey.LAND_OF_DEATH_REVIVE_FAILED], player.CharacterId);
                }
            });
        }

        public async Task ReturnToEntranceAsync(ClientSession session)
        {
            var player = session.Character;
            await player.OnMapAsync(() =>
            {
                player.Hp = 1;
                player.Mp = 1;
                player.IsAlive = true;
                return Task.CompletedTask;
            });
            if (player.MapInstance is { } mapInstance)
            {
                await mapInstance.SendPacketAsync(new RevivePacket
                {
                    VisualType = VisualType.Player,
                    VisualId = player.VisualId,
                    Data = 0,
                });
            }

            await player.SendPacketAsync(player.GenerateStat());
            await mapChangeService.ChangeMapAsync(session, player.MapId, player.MapX, player.MapY);
            await player.SendPacketAsync(Message(Game18NConstString.ReturnToEntranceLandOfDeath));
        }

        private async Task RunAsync()
        {
            _gateOpen = true;
            var started = Stopwatch.GetTimestamp();
            for (var step = 0; (_remaining = Length - ClockRefresh * step) > TimeSpan.Zero; step++)
            {
                var lands = _lands.Values.ToList();
                foreach (var land in lands)
                {
                    await land.SendPacketAsync(Clock(_remaining));
                }

                if (IsHuntStart(_remaining) || IsHuntEnd(_remaining))
                {
                    await _landLock.WaitAsync();
                    try
                    {
                        _huntActive = IsHuntStart(_remaining);
                        if (!_huntActive)
                        {
                            _gateOpen = false;
                        }
                        // A land nobody has reached yet gets its devil from the entry path, on its first arrival.
                        foreach (var land in _lands.Values.Where(l => !_huntActive
                                     || sessionRegistry.GetClientSessionsByMapInstance(l.MapInstanceId).Any()))
                        {
                            await land.Executor.RunAsync(() => _huntActive ? StartHuntAsync(land) : EndHuntAsync(land));
                        }
                    }
                    finally
                    {
                        _landLock.Release();
                    }
                }

                if (_remaining == ClosingWarning)
                {
                    foreach (var land in lands)
                    {
                        await land.SendPacketAsync(Message(Game18NConstString.LandOfDeathCloseSoon));
                    }
                }

                var wait = ClockRefresh * (step + 1) - Stopwatch.GetElapsedTime(started);
                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait);
                }
            }
        }

        private async Task<MapInstance?> GetOrCreateLandAsync(long familyId)
        {
            if (_lands.TryGetValue(familyId, out var existing))
            {
                return existing;
            }

            await _landLock.WaitAsync();
            try
            {
                if (_lands.TryGetValue(familyId, out existing))
                {
                    return existing;
                }

                var baseMap = mapInstanceRegistry.GetBaseMapById(MapId);
                if (baseMap == null)
                {
                    logger.LogError(logLanguage[LogLanguageKey.LAND_OF_DEATH_MAP_MISSING], MapId);
                    return null;
                }

                var land = mapInstanceGeneratorService.Value.CreateMapInstance(baseMap.Map, Guid.NewGuid(), false,
                    MapInstanceType.LodInstance);
                land.LoadMonsters(mapMonsters.Where(m => m.MapId == MapId)?.ToList() ?? [], npcMonsters);
                await mapInstanceGeneratorService.Value.AddMapInstanceAsync(land);
                await land.StartLifeAsync();
                _lands[familyId] = land;
                return land;
            }
            finally
            {
                _landLock.Release();
            }
        }

        private async Task StartHuntAsync(MapInstance land)
        {
            var npcMonster = npcMonsters.Find(m => m.NpcMonsterVNum == FlyingFireDevilVNum);
            if (npcMonster == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.LAND_OF_DEATH_MONSTER_MISSING], FlyingFireDevilVNum);
                return;
            }

            var prey = _lastArrival.TryGetValue(land.MapInstanceId, out var characterId)
                ? sessionRegistry.GetClientSessionsByMapInstance(land.MapInstanceId)
                    .FirstOrDefault(s => s.HasPlayerEntity && s.Character.CharacterId == characterId)?.Character
                : null;
            var devil = land.SpawnMonster(npcMonster, prey?.PositionX ?? DevilDefaultX, prey?.PositionY ?? DevilDefaultY, true);
            _devils[land.MapInstanceId] = devil;
            land.XpRate = HuntRate;
            land.DropRate = HuntRate;
            await land.SendPacketAsync(devil.GenerateIn());
            if (prey is { } target)
            {
                aggroService.AddThreat(devil, target, 1);
            }

            await land.SendPacketAsync(Message(Game18NConstString.FlyingFireDevilAppears));
        }

        private async Task EndHuntAsync(MapInstance land)
        {
            land.XpRate = 1;
            land.DropRate = 1;
            if (_devils.TryRemove(land.MapInstanceId, out var devil) && devil.IsAlive)
            {
                devil.IsAlive = false;
                aggroService.Clear(devil);
                await land.SendPacketAsync(new OutPacket { VisualType = VisualType.Monster, VisualId = devil.VisualId });
            }

            await land.SendPacketAsync(Message(Game18NConstString.FlyingFireDevilVanished));
        }

        private async Task CloseAllAsync()
        {
            foreach (var (familyId, land) in _lands.ToList())
            {
                try
                {
                    var players = sessionRegistry.GetClientSessionsByMapInstance(land.MapInstanceId).ToList();
                    await mapInstanceGeneratorService.Value.RemoveMapAsync(land.MapInstanceId);
                    foreach (var player in players)
                    {
                        await player.SendPacketAsync(Message(Game18NConstString.OutOfLandOfDeath));
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, logLanguage[LogLanguageKey.LAND_OF_DEATH_CLOSE_FAILED], familyId);
                }
                finally
                {
                    _lands.TryRemove(familyId, out _);
                    _lastArrival.TryRemove(land.MapInstanceId, out _);
                    _devils.TryRemove(land.MapInstanceId, out _);
                }
            }
        }

        public Task LeaveAsync(ClientSession session) =>
            session.SendPacketAsync(new EventPacket { Type = EventPacketType.ClearTimeSpaceUI });

        private static EventPacket Clock(TimeSpan remaining)
        {
            var deciseconds = (int)(remaining.TotalMilliseconds / 100);
            return new EventPacket
            {
                Type = EventPacketType.TSTimer,
                IsDisabled = false,
                Parameter = deciseconds,
                SecondParameter = deciseconds,
            };
        }

        private static MsgiPacket Message(Game18NConstString message) => new() { Type = MessageType.Default, Message = message };
    }
}
