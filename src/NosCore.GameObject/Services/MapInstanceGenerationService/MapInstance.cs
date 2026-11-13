//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Arch.Core;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.DigestionService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Interfaces;
using NosCore.PathFinder.Interfaces;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapInstanceGenerationService
{
    public class MapInstance : IBroadcastable, IDisposable, IAsyncDisposable
    {
        public short MaxPacketsBuffer { get; } = 250;
        private readonly ILogger<MapInstance> _logger;

        private readonly IMapItemGenerationService _mapItemGenerationService;
        private readonly Lock _sleepLock = new();
        private bool _isSleeping;
        private bool _isSleepingRequest;
        private ConcurrentDictionary<int, MonsterComponentBundle> _monsters;

        private ConcurrentDictionary<int, NpcComponentBundle> _npcs;

        private readonly MapItemSystem _mapItemSystem;

        public ConcurrentDictionary<Guid, MapDesignObject> MapDesignObjects = new();

        private readonly IClock _clock;
        private readonly IMapChangeService _mapChangeService;
        private readonly ISessionRegistry _sessionRegistry;
        private readonly IHeuristic _distanceCalculator;
        private readonly IMonsterAi? _monsterAi;
        private readonly IBuffService? _buffService;
        private readonly IRegenerationService? _regenerationService;
        private readonly IVitalityService? _vitalityService;
        private readonly IDigestionService? _digestionService;
        private readonly ISpeedCalculationService? _speedCalculationService;
        private readonly IBattleStatsProvider? _battleStatsProvider;
        private readonly IHeatService? _heatService;
        private readonly RespawnSystem _respawnSystem = new();
        private readonly List<Entity> _dueRespawns = new();
        private readonly BuffSystem _buffSystem = new();
        private readonly SkillResetSystem _skillResetSystem = new();
        private readonly List<(Entity Entity, long CastId)> _readySkills = new();
        private readonly List<(Entity Entity, VisualType VisualType)> _dueBuffs = new();

        public MapWorld EcsWorld { get; }

        public MapExecutor Executor { get; } = new();

        public MapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType type,
            IMapItemGenerationService mapItemGenerationService, ILogger<MapInstance> logger, IClock clock, IMapChangeService mapChangeService,
            ISessionGroupFactory sessionGroupFactory, ISessionRegistry sessionRegistry, IHeuristic distanceCalculator,
            IMonsterAi? monsterAi = null, IBuffService? buffService = null, IRegenerationService? regenerationService = null,
            IVitalityService? vitalityService = null,
            IDigestionService? digestionService = null, ISpeedCalculationService? speedCalculationService = null,
            IBattleStatsProvider? battleStatsProvider = null,
            IHeatService? heatService = null)
        {
            LastPackets = new ConcurrentQueue<IPacket>();
            XpRate = 1;
            DropRate = 1;
            ShopAllowed = shopAllowed;
            MapInstanceType = type;
            Map = map;
            MapInstanceId = guid;
            Portals = new List<Portal>();
            _monsters = new ConcurrentDictionary<int, MonsterComponentBundle>();
            _npcs = new ConcurrentDictionary<int, NpcComponentBundle>();
            _mapItemSystem = new MapItemSystem();
            _isSleeping = true;
            _clock = clock;
            LastUnregister = _clock.GetCurrentInstant().Plus(Duration.FromMinutes(-1));
            Sessions = sessionGroupFactory.Create();
            _mapItemGenerationService = mapItemGenerationService;
            _logger = logger;
            _mapChangeService = mapChangeService;
            _sessionRegistry = sessionRegistry;
            _distanceCalculator = distanceCalculator;
            _monsterAi = monsterAi;
            _buffService = buffService;
            _regenerationService = regenerationService;
            _vitalityService = vitalityService;
            _digestionService = digestionService;
            _speedCalculationService = speedCalculationService;
            _battleStatsProvider = battleStatsProvider;
            _heatService = heatService;
            EcsWorld = new MapWorld();
        }

        public Instant LastUnregister { get; set; }

        public int MapItemCount => _mapItemSystem.Count(EcsWorld);

        public IEnumerable<MapItemComponentBundle> GetMapItemBundles()
        {
            return _mapItemSystem.GetAll(EcsWorld)
                .Select(entity => new MapItemComponentBundle(entity, EcsWorld));
        }

        public MapItemComponentBundle? TryGetMapItem(long visualId)
        {
            return _mapItemSystem.Find(EcsWorld, visualId) is { } entity
                ? new MapItemComponentBundle(entity, EcsWorld)
                : null;
        }

        // Only one of several callers racing for the same item gets true.
        public bool TryRemoveMapItem(long visualId)
        {
            return _mapItemSystem.Find(EcsWorld, visualId) is { } entity && EcsWorld.DestroyEntity(entity);
        }

        // A map with somebody on it is never asleep, whatever order a leaver's request to sleep
        // and an arrival's wake-up came in.
        public bool IsSleeping
        {
            get
            {
                lock (_sleepLock)
                {
                    if (Sessions.Count > 0)
                    {
                        _isSleeping = false;
                        _isSleepingRequest = false;
                        return false;
                    }

                    if (!_isSleepingRequest || _isSleeping || (LastUnregister.Plus(Duration.FromSeconds(30)) >= _clock.GetCurrentInstant()))
                    {
                        return _isSleeping;
                    }

                    _isSleeping = true;
                    _isSleepingRequest = false;
                    return true;
                }
            }
            set
            {
                lock (_sleepLock)
                {
                    if (value)
                    {
                        _isSleepingRequest = true;
                    }
                    else
                    {
                        _isSleeping = false;
                        _isSleepingRequest = false;
                    }
                }
            }
        }

        public int DropRate { get; set; }

        public Map.Map Map { get; set; }

        public Guid MapInstanceId { get; set; }

        public MapInstanceType MapInstanceType { get; set; }

        public byte MapIndexX { get; set; }

        public byte MapIndexY { get; set; }

        public IEnumerable<MonsterComponentBundle> EnumerateMonsters() => _monsters.EnumerateValues();

        public IEnumerable<NpcComponentBundle> EnumerateNpcs() => _npcs.EnumerateValues();

        public List<Portal> Portals { get; set; }

        public bool ShopAllowed { get; }

        public int XpRate { get; set; }

        private readonly Lock _lifeLock = new();
        private CancellationTokenSource? _lifeCts;
        private Task? _lifeLoop;

        public ISessionGroup Sessions { get; set; }

        public ConcurrentQueue<IPacket> LastPackets { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task KickAsync()
        {
            return KickAsync(_ => true);
        }

        public async Task KickAsync(Func<ClientSession, bool> filter)
        {
            var sessions = _sessionRegistry.GetClientSessionsByMapInstance(MapInstanceId)
                .Where(s => s.HasPlayerEntity && !s.Character.IsDisconnecting && filter(s))
                .ToList();

            foreach (var session in sessions)
            {
                var character = session.Character;
                await _mapChangeService.ChangeMapAsync(session, character.MapId, character.MapX, character.MapY);
            }
        }

        public MapItemComponentBundle? PutItem(short amount, IItemInstance inv, ClientSession session)
        {
            var random2 = Guid.NewGuid();
            var possibilities = new List<(short X, short Y)>();

            for (short x = -1; x < 2; x++)
            {
                for (short y = -1; y < 2; y++)
                {
                    possibilities.Add((x, y));
                }
            }

            short mapX = 0;
            short mapY = 0;
            var niceSpot = false;
            var orderedPossibilities = possibilities.OrderBy(_ => RandomHelper.Instance.RandomNumber()).ToList();
            for (var i = 0; (i < orderedPossibilities.Count) && !niceSpot; i++)
            {
                mapX = (short)(session.Character.PositionX + orderedPossibilities[i].X);
                mapY = (short)(session.Character.PositionY + orderedPossibilities[i].Y);
                if (Map.IsBlockedZone(session.Character.PositionX, session.Character.PositionY, mapX, mapY))
                {
                    continue;
                }

                niceSpot = true;
            }

            if (!niceSpot)
            {
                return null;
            }

            if ((amount <= 0) || (amount > inv.Amount))
            {
                return null;
            }

            var newItemInstance = (IItemInstance)inv.Clone();
            newItemInstance.Id = random2;
            newItemInstance.Amount = amount;
            var droppedItem = _mapItemGenerationService.Create(this, newItemInstance, mapX, mapY);
            inv.Amount -= amount;
            if (inv.Amount == 0)
            {
                session.Character.InventoryService.DeleteById(inv.Id);
            }

            return droppedItem;
        }

        public void LoadMonsters(List<MapMonsterDto> monsters, List<NpcMonsterDto> npcMonsters)
        {
            var entries = new Dictionary<int, MonsterComponentBundle>();
            foreach (var x in monsters)
            {
                var npcMonster = npcMonsters.Find(o => o.NpcMonsterVNum == x.VNum);
                if (npcMonster == null)
                {
                    continue;
                }

                var entity = EcsWorld.CreateMonster(
                    x.MapMonsterId,
                    npcMonster,
                    this,
                    x.MapX, x.MapY, x.Direction,
                    x.MapX, x.MapY,
                    npcMonster.CanWalk, false, x.IsDisabled);
                entries[x.MapMonsterId] = new MonsterComponentBundle(entity, EcsWorld);
            }
            _monsters = new ConcurrentDictionary<int, MonsterComponentBundle>(entries);
        }

        public void LoadNpcs(List<MapNpcDto> npcs, List<NpcMonsterDto> npcMonsters)
        {
            var entries = new Dictionary<int, NpcComponentBundle>();
            foreach (var x in npcs)
            {
                var npcMonster = npcMonsters.Find(o => o.NpcMonsterVNum == x.VNum);
                if (npcMonster == null)
                {
                    continue;
                }

                var entity = EcsWorld.CreateNpc(
                    x.MapNpcId,
                    npcMonster,
                    this,
                    x.MapX, x.MapY, x.Direction,
                    x.MapX, x.MapY,
                    x.IsMoving, x.IsDisabled, x.Dialog, x.Effect, x.EffectDelay,
                    null);
                entries[x.MapNpcId] = new NpcComponentBundle(entity, EcsWorld);
            }
            _npcs = new ConcurrentDictionary<int, NpcComponentBundle>(entries);
        }

        public NpcComponentBundle? GetNpcById(int mapNpcId) =>
            _npcs.TryGetValue(mapNpcId, out var n) ? n : null;

        public MonsterComponentBundle? GetMonsterById(int mapMonsterId) =>
            _monsters.TryGetValue(mapMonsterId, out var m) ? m : null;

        public NpcComponentBundle? FindNpc(Func<NpcComponentBundle, bool> predicate)
        {
            foreach (var n in _npcs.Values)
            {
                if (predicate(n)) return n;
            }
            return null;
        }

        public MonsterComponentBundle? FindMonster(Func<MonsterComponentBundle, bool> predicate)
        {
            foreach (var m in _monsters.Values)
            {
                if (predicate(m)) return m;
            }
            return null;
        }

        public List<IPacket> GetMapItems(RegionType language)
        {
            var packets = new List<IPacket>();
            // TODO: Parallelize getting of items of mapinstance
            Portals.ForEach(s => packets.Add(s.GenerateGp()));

            foreach (var s in _monsters.EnumerateValues())
            {
                packets.Add(s.GenerateIn());

                if (s.Size != 10)
                {
                    packets.Add(s.GenerateCharSc());
                }
            }

            foreach (var s in _npcs.EnumerateValues())
            {
                if (!s.IsAlive)
                {
                    continue;
                }

                packets.Add(s.GenerateIn());

                if (s.Shop != null)
                {
                    var shopPacket = s.GenerateShop(language);
                    packets.Add(shopPacket);
                }

                if (s.Size != 10)
                {
                    packets.Add(s.GenerateCharSc());
                }
            }

            foreach (var mapItem in GetMapItemBundles())
            {
                packets.Add(mapItem.GenerateIn());
            }
            return packets;
        }

        // The client lists the placed objects next to a visitor from this table; the owner fills it from mlobjlst.
        public MltobjPacket GenerateMltobj()
        {
            return new MltobjPacket
            {
                MltobjSubPacket = MapDesignObjects.Values
                    .OrderBy(o => o.Slot)
                    .Select(o => (MltobjSubPacket?)new MltobjSubPacket
                    {
                        VNum = o.InventoryItemInstance?.ItemInstance.ItemVNum ?? 0,
                        Slot = o.Slot,
                        MapX = o.MapX,
                        MapY = o.MapY
                    })
                    .ToList()
            };
        }

        public CMapPacket GenerateCMap(bool enter)
        {
            return new CMapPacket
            {
                Type = 0,
                Id = Map.MapId,
                IsEntering = enter
            };
        }

        // One awaited loop per map replaces the per-entity 400ms timers: entity AI
        // steps run sequentially inside the tick, so a slow tick delays the next one
        // instead of overlapping it, and a sleeping map costs one early-out per tick
        // instead of thousands of idle timers.
        public Task StartLifeAsync()
        {
            lock (_lifeLock)
            {
                if (_lifeLoop == null)
                {
                    _lifeCts = new CancellationTokenSource();
                    _lifeLoop = RunLifeLoopAsync(_lifeCts.Token);
                }
            }

            return Task.CompletedTask;
        }

        private async Task RunLifeLoopAsync(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(400));
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    try
                    {
                        await Executor.RunAsync(TickLifeAsync);
                    }
                    catch (ObjectDisposedException) when (Executor.IsClosed)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Life tick failed for map {MapId}", Map.MapId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        // Every step of the tick is guarded on its own, so one faulty entity or service does
        // not cost the rest of the map its buffs, regeneration or respawns for that tick.
        private async Task TickLifeAsync()
        {
            if (IsSleeping)
            {
                return;
            }

            foreach (var (_, monster) in _monsters)
            {
                try
                {
                    await monster.TickLifeAsync(_monsterAi, _distanceCalculator, _clock, _logger);
                }
                catch (Exception e)
                {
                    LogTickFailure(e, "monster", monster.VisualId);
                }
            }

            foreach (var (_, npc) in _npcs)
            {
                try
                {
                    await npc.TickLifeAsync(_monsterAi, _distanceCalculator, _clock, _logger);
                }
                catch (Exception e)
                {
                    LogTickFailure(e, "npc", npc.VisualId);
                }
            }

            await GuardAsync(_buffService is { } buffs ? () => TickBuffsAsync(buffs) : null, "buffs");
            await GuardAsync(_regenerationService == null ? null : () => _regenerationService.TickAsync(this), "regeneration");
            await GuardAsync(_digestionService == null ? null : () => _digestionService.TickAsync(this), "digestion");
            await GuardAsync(SendSkillResetsAsync, "cooldown resets");
            await GuardAsync(SweepPendingRespawnsAsync, "respawns");
        }

        private async Task TickBuffsAsync(IBuffService buffs)
        {
            _dueBuffs.Clear();
            _buffSystem.FindDue(EcsWorld, _clock.GetCurrentInstant(), _dueBuffs);
            foreach (var (entity, visualType) in _dueBuffs)
            {
                try
                {
                    switch (visualType)
                    {
                        case VisualType.Monster:
                            await buffs.TickAsync(new MonsterComponentBundle(entity, EcsWorld));
                            break;
                        case VisualType.Npc:
                            await buffs.TickAsync(new NpcComponentBundle(entity, EcsWorld));
                            break;
                        case VisualType.Player:
                            await TickPlayerBuffsAsync(buffs, new PlayerComponentBundle(entity, EcsWorld));
                            break;
                    }
                }
                catch (Exception e)
                {
                    LogTickFailure(e, $"{visualType} buffs", EcsWorld.TryGetComponent<EntityIdentityComponent>(entity)?.VisualId ?? 0);
                }
            }
        }

        private async Task TickPlayerBuffsAsync(IBuffService buffs, PlayerComponentBundle character)
        {
            var expired = await buffs.TickAsync(character);
            if (expired.Count == 0)
            {
                return;
            }

            if (_vitalityService != null)
            {
                await _vitalityService.RefreshAndNotifyAsync(character);
            }

            if (_battleStatsProvider != null)
            {
                await character.SendPacketAsync(character.GenerateSc(_battleStatsProvider));
            }

            if (_speedCalculationService != null && character.IsVehicled)
            {
                character.Speed = _speedCalculationService.CalculateSpeed(character);
                await character.SendPacketAsync(character.GenerateCond());
            }

            if (_heatService != null)
            {
                await _heatService.RefreshAsync(character);
            }
        }

        private async Task SendSkillResetsAsync()
        {
            _readySkills.Clear();
            _skillResetSystem.TakeReady(EcsWorld, _clock.GetCurrentInstant(), _readySkills);
            foreach (var (entity, castId) in _readySkills)
            {
                var character = new PlayerComponentBundle(entity, EcsWorld);
                if (character.IsDisconnecting)
                {
                    continue;
                }

                try
                {
                    await character.SendPacketAsync(new SkillResetPacket { CastId = castId });
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to reset cooldown for skill {CastId}", castId);
                }
            }
        }

        private async Task GuardAsync(Func<Task>? step, string what)
        {
            if (step == null)
            {
                return;
            }

            try
            {
                await step();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Life tick step {Step} failed for map {MapId}", what, Map.MapId);
            }
        }

        private void LogTickFailure(Exception e, string what, long id)
        {
            _logger.LogError(e, "Life tick failed for {What} {Id} on map {MapId}", what, id, Map.MapId);
        }

        private async Task SweepPendingRespawnsAsync()
        {
            _dueRespawns.Clear();
            _respawnSystem.TakeDue(EcsWorld, _clock.GetCurrentInstant(), _dueRespawns);
            foreach (var entity in _dueRespawns)
            {
                var visualType = EcsWorld.TryGetComponent<EntityIdentityComponent>(entity)?.VisualType;
                try
                {
                    if (visualType == VisualType.Monster)
                    {
                        var monster = new MonsterComponentBundle(entity, EcsWorld);
                        monster.Respawn();
                        await this.SendPacketAsync(monster.GenerateIn());
                    }
                    else if (visualType == VisualType.Npc)
                    {
                        var npc = new NpcComponentBundle(entity, EcsWorld);
                        npc.IsAlive = true;
                        await this.SendPacketAsync(npc.GenerateIn());
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Life tick failed for respawn {Entity} on map {MapId}", entity, Map.MapId);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeCoreAsync();
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            DisposeCoreAsync().GetAwaiter().GetResult();
        }

        private async Task DisposeCoreAsync()
        {
            Task? lifeLoop;
            lock (_lifeLock)
            {
                _lifeCts?.Cancel();
                lifeLoop = _lifeLoop;
            }

            // A tick already running walks the entities of the world below, so it has to
            // finish before the world goes - unless the tick is what is disposing the map.
            if (!Executor.IsCurrent && lifeLoop != null)
            {
                await lifeLoop;
            }

            await Executor.CloseAsync();

            lock (_lifeLock)
            {
                _lifeCts?.Dispose();
                _lifeCts = null;
                _lifeLoop = null;
            }

            EcsWorld.Dispose();
        }
    }
}
