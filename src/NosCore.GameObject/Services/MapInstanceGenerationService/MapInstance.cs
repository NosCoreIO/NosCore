//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Interfaces;
using NosCore.PathFinder.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;


namespace NosCore.GameObject.Services.MapInstanceGenerationService
{
    public class MapInstance : IBroadcastable, IDisposable
    {
        public short MaxPacketsBuffer { get; } = 250;
        private readonly ILogger<MapInstance> _logger;

        private readonly IMapItemGenerationService _mapItemGenerationService;
        private bool _isSleeping;
        private bool _isSleepingRequest;
        private ConcurrentDictionary<int, MonsterComponentBundle> _monsters;

        private ConcurrentDictionary<int, NpcComponentBundle> _npcs;

        private readonly VisibilitySystem _visibilitySystem;

        public ConcurrentDictionary<Guid, MapDesignObject> MapDesignObjects = new();

        private readonly IClock _clock;
        private readonly IMapChangeService _mapChangeService;
        private readonly ISessionRegistry _sessionRegistry;
        private readonly IHeuristic _distanceCalculator;
        private readonly IMonsterAi? _monsterAi;
        private readonly IBuffService? _buffService;
        private readonly IRegenerationService? _regenerationService;

        public MapWorld EcsWorld { get; }

        public MapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType type,
            IMapItemGenerationService mapItemGenerationService, ILogger<MapInstance> logger, IClock clock, IMapChangeService mapChangeService,
            ISessionGroupFactory sessionGroupFactory, ISessionRegistry sessionRegistry, IHeuristic distanceCalculator,
            IMonsterAi? monsterAi = null, IBuffService? buffService = null, IRegenerationService? regenerationService = null)
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
            _visibilitySystem = new VisibilitySystem();
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
            EcsWorld = new MapWorld();
        }

        public Instant LastUnregister { get; set; }

        public int MapItemCount => GetMapItemBundles().Count();

        public IEnumerable<MapItemComponentBundle> GetMapItemBundles()
        {
            return _visibilitySystem.GetMapItemEntities(EcsWorld)
                .Select(entity => new MapItemComponentBundle(entity, EcsWorld));
        }

        public MapItemComponentBundle? TryGetMapItem(long visualId)
        {
            foreach (var bundle in GetMapItemBundles())
            {
                if (bundle.VisualId == visualId)
                {
                    return bundle;
                }
            }
            return null;
        }

        public bool TryRemoveMapItem(long visualId)
        {
            foreach (var entity in _visibilitySystem.GetMapItemEntities(EcsWorld))
            {
                var identity = EcsWorld.TryGetComponent<EntityIdentityComponent>(entity);
                if (identity?.VisualId == visualId)
                {
                    EcsWorld.DestroyEntity(entity);
                    return true;
                }
            }
            return false;
        }

        public bool IsSleeping
        {
            get
            {
                if (!_isSleepingRequest || _isSleeping || (LastUnregister.Plus(Duration.FromSeconds(30)) >= _clock.GetCurrentInstant()))
                {
                    return _isSleeping;
                }

                _isSleeping = true;
                _isSleepingRequest = false;
                Parallel.ForEach(Monsters.Where(s => s.Life != null), monster => NonPlayableEntityExtension.StopLife(monster));
                Parallel.ForEach(Npcs.Where(s => s.Life != null), npc => NonPlayableEntityExtension.StopLife(npc));

                return true;
            }
            set
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

        public int DropRate { get; set; }

        public Map.Map Map { get; set; }

        public Guid MapInstanceId { get; set; }

        public MapInstanceType MapInstanceType { get; set; }

        public List<MonsterComponentBundle> Monsters
        {
            get { return _monsters.Select(s => s.Value).ToList(); }
        }

        public List<NpcComponentBundle> Npcs
        {
            get { return _npcs.Select(s => s.Value).ToList(); }
        }

        public List<Portal> Portals { get; set; }

        public bool ShopAllowed { get; }

        public int XpRate { get; set; }

        private IDisposable? Life { get; set; }

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

            Monsters.ForEach(s =>
            {
                packets.Add(s.GenerateIn());

                if (s.Size != 10)
                {
                    packets.Add(s.GenerateCharSc());
                }
            });

            Npcs.ForEach(s =>
            {
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
            });
            foreach (var mapItem in GetMapItemBundles())
            {
                packets.Add(mapItem.GenerateIn());
            }
            return packets;
        }

        public CMapPacket GenerateCMap(bool enter)
        {
            return new CMapPacket
            {
                Type = 0,
                Id = Map.MapId,
                MapType = enter
            };
        }

        public Task StartLifeAsync()
        {
            async Task LifeAsync()
            {
                try
                {
                    if (IsSleeping)
                    {
                        return;
                    }

                    await Task.WhenAll(Monsters.Where(s => s.Life == null).Select(monster => monster.StartLifeAsync(_monsterAi, _distanceCalculator, _clock, _logger)));
                    await Task.WhenAll(Npcs.Where(s => s.Life == null).Select(npc => npc.StartLifeAsync(_monsterAi, _distanceCalculator, _clock, _logger)));

                    // Buff expiration: drop any buff whose ExpiresAt is past. Done
                    // per-map so the tick rate matches the life loop (400ms) which is
                    // fine for buffs since their Duration is measured in deciseconds.
                    if (_buffService != null)
                    {
                        foreach (var monster in Monsters) await _buffService.TickAsync(monster).ConfigureAwait(false);
                        foreach (var npc in Npcs) await _buffService.TickAsync(npc).ConfigureAwait(false);
                        foreach (var session in _sessionRegistry.GetClientSessionsByMapInstance(MapInstanceId))
                        {
                            if (session.HasPlayerEntity)
                            {
                                await _buffService.TickAsync(session.Character).ConfigureAwait(false);
                            }
                        }
                    }

                    // HP/MP regen for connected players on this map — matches OpenNos
                    // Character regen block (sitting 1.5s cadence, standing 2s). The
                    // service throttles internally so calling each 400ms tick is safe.
                    if (_regenerationService != null)
                    {
                        await _regenerationService.TickAsync(this).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                }
            }
            Life = Observable.Interval(TimeSpan.FromMilliseconds(400)).Select(_ => LifeAsync()).Subscribe();
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Parallel.ForEach(Monsters.Where(s => s.Life != null), monster => NonPlayableEntityExtension.StopLife(monster));
            Parallel.ForEach(Npcs.Where(s => s.Life != null), npc => NonPlayableEntityExtension.StopLife(npc));

            Life?.Dispose();
            Life = null;
            EcsWorld.Dispose();
        }
    }
}
