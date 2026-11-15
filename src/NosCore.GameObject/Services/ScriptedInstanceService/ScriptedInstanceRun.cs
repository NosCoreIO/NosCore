//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Instances.Abstractions;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public class ScriptedInstanceRun : IInstanceRun
    {
        private static readonly Duration ReturnDelay = Duration.FromSeconds(10);
        private const int EscortReach = 4;
        private const int EscortJoinedEffect = 5004;
        private const byte EscortJoinedAction = 3;

        private const short BonusEffect = 826;
        private const short BonusEffectDelay = 5000;

        private readonly IInstanceRunHost _host;
        private readonly IInstanceScript _script;
        private readonly IReadOnlyDictionary<int, MapInstance> _rooms;
        private readonly IReadOnlyDictionary<short, NpcMonsterDto> _npcMonsters;
        private readonly IRandomProvider _random;
        private readonly IClock _clock;
        private readonly ISessionRegistry _sessionRegistry;
        private readonly IAggroService _aggroService;
        private readonly IItemThrowService _itemThrowService;
        private readonly ILogger _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly List<Func<Task>> _pending = [];
        private readonly Dictionary<int, short> _lockers = [];
        private readonly Dictionary<int, short> _lockerVNums = [];
        private readonly HashSet<int> _visited = [];
        private readonly Dictionary<int, int> _aliveByRoom = [];
        private readonly HashSet<long> _down = [];
        private readonly Dictionary<(ObjectiveKind Kind, short VNum), int> _objectiveProgress = [];

        private readonly ConcurrentDictionary<long, byte> _chestsOpened = new();
        private readonly Dictionary<int, long> _newestArrival = [];
        private readonly List<(MapInstance Room, int NpcId)> _escorts = [];
        private readonly List<(Duration At, Action<IInstanceRun> Step)> _scheduled = [];
        private Instant? _scriptStartedAt;
        private int _nextPortalId;
        private int _combo;
        private bool _started;
        private Instant? _clockEndsAt;
        private Duration _clockLimit;
        private Instant? _returnAt;
        private readonly TimeSpan _stepTimeout;
        private volatile bool _abandoned;
        private bool _endingApplied;

        public ScriptedInstanceRun(IInstanceRunHost host, IInstanceScript script,
            IReadOnlyDictionary<int, MapInstance> rooms, IReadOnlyDictionary<short, NpcMonsterDto> npcMonsters,
            IRandomProvider random, IClock clock, ISessionRegistry sessionRegistry, IAggroService aggroService,
            IItemThrowService itemThrowService, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, TimeSpan stepTimeout)
        {
            _stepTimeout = stepTimeout;
            _host = host;
            _script = script;
            _rooms = rooms;
            _npcMonsters = npcMonsters;
            _random = random;
            _clock = clock;
            _sessionRegistry = sessionRegistry;
            _aggroService = aggroService;
            _itemThrowService = itemThrowService;
            _logger = logger;
            _logLanguage = logLanguage;
            Rooms = rooms.ToDictionary(s => s.Key, s => s.Value.MapInstanceId);
            StartedAt = clock.GetCurrentInstant();
        }

        public IInstanceRunHost Host => _host;

        public string Name => _host.Name;

        public IReadOnlyDictionary<int, Guid> Rooms { get; }

        private int _closed;

        // True for exactly one caller: the one that gets to tear the rooms down.
        public bool TryClose() => Interlocked.Exchange(ref _closed, 1) == 0;

        public Instant StartedAt { get; }

        public EndStateType? EndState { get; private set; }

        public bool HasStarted => _started;

        public int MonstersSummoned { get; private set; }

        public int MonstersKilled { get; private set; }

        public int NpcsSummoned { get; private set; }

        // Each kill scores; a run of bonus kills scores more the longer it lasts.
        public int Points { get; private set; }

        public int RoomsVisited => _visited.Count;

        public TimeSpan Elapsed => _scriptStartedAt is { } startedAt
            ? (_clock.GetCurrentInstant() - startedAt).ToTimeSpan()
            : TimeSpan.Zero;

        public Task StartAsync()
        {
            return RunAsync(async () =>
            {
                if (_started || _abandoned)
                {
                    return;
                }

                await BeginAsync();
            });
        }

        private Task BeginAsync()
        {
            _started = true;
            _scriptStartedAt = _clock.GetCurrentInstant().Minus(Duration.FromTimeSpan(_host.AlreadyElapsed));
            return CallScriptAsync(run => _script.OnFirstEnter(run));
        }

        public Task MonsterKilledAsync(Guid mapInstanceId, short vNum, bool isBonus = false,
            Action<IInstanceRun>? deathStep = null)
        {
            return RunAsync(async () =>
            {
                var room = RoomOf(mapInstanceId);
                if (room == null)
                {
                    return;
                }

                MonstersKilled++;
                Advance(ObjectiveKind.KillMonster, vNum);
                _combo = isBonus ? _combo + 1 : 0;
                Points += ComboPoints(isBonus ? _combo + 1 : 0);
                var aliveBefore = _aliveByRoom.GetValueOrDefault(room.Value);
                _aliveByRoom[room.Value] = Math.Max(0, aliveBefore - 1);
                if (_abandoned)
                {
                    return;
                }

                await CallScriptAsync(run => _script.OnMonsterKilled(run, room.Value, vNum));
                if (deathStep != null)
                {
                    await CallScriptAsync(deathStep);
                }

                if (!_lockers.TryGetValue(room.Value, out var remaining))
                {
                    if (aliveBefore > 0 && _aliveByRoom[room.Value] == 0 && EndState == null)
                    {
                        await CallScriptAsync(run => _script.OnRoomCleared(run, room.Value));
                    }

                    return;
                }

                if (remaining <= 0 || (_lockerVNums.TryGetValue(room.Value, out var counted) && counted != vNum))
                {
                    return;
                }

                _lockers[room.Value] = --remaining;
                if (remaining == 0)
                {
                    await CallScriptAsync(run => _script.OnRoomCleared(run, room.Value));
                }
            });
        }

        private void Advance(ObjectiveKind kind, short vNum)
        {
            _objectiveProgress[(kind, vNum)] = _objectiveProgress.GetValueOrDefault((kind, vNum)) + 1;
        }

        private static int ComboPoints(int combo)
        {
            var (points, next) = (4, 7);
            for (var i = 0; i < combo; i++)
            {
                (points, next) = (next, points + next);
            }

            return points;
        }

        public Task ExitReachedAsync(Guid mapInstanceId)
        {
            return RunAsync(async () =>
            {
                if (RoomOf(mapInstanceId) is not { } room || EndState != null || _abandoned)
                {
                    return;
                }

                if (_script.Definition.Objectives.Any(o => _objectiveProgress.GetValueOrDefault((o.Kind, o.VNum)) < o.Count))
                {
                    var exitRoom = _rooms[room];
                    _pending.Add(() => OnRoom(exitRoom, () => exitRoom.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.HaveIncompleteMission
                    })));
                    return;
                }

                await CallScriptAsync(run => _script.OnExitReached(run, room));
            });
        }

        // Lets a host change its own state from outside the run's triggers, such as from a packet.
        public Task RunHostAsync(Func<Task> work)
        {
            return RunAsync(() =>
            {
                if (EndState == null)
                {
                    _pending.Add(work);
                }

                return Task.CompletedTask;
            });
        }

        // The script starts once the first player has finished entering, so what it summons
        // reaches a client that is already on the map.
        public Task EnteredAsync(ClientSession session, Guid mapInstanceId)
        {
            return RunAsync(async () =>
            {
                var room = RoomOf(mapInstanceId);
                var discovered = room != null && _visited.Add(room.Value);
                if (room != null && session.HasPlayerEntity)
                {
                    _newestArrival[room.Value] = session.Character.CharacterId;
                    BringEscorts(session.Character, room.Value);
                }

                if (!_started && !_abandoned && _host.StartsOnFirstEntry)
                {
                    await BeginAsync();
                }

                if (discovered && room is { } entered && _started && !_abandoned && EndState == null)
                {
                    await CallScriptAsync(run => _script.OnRoomEntered(run, entered));
                }

                if (_clockEndsAt is { } endsAt && EndState == null)
                {
                    _pending.Add(() => session.SendPacketAsync(GenerateClock(endsAt)));
                }
            });
        }

        public Task PlayerDownAsync(PlayerComponentBundle player)
        {
            return RunAsync(() =>
            {
                if (player.MapInstance is { } map && RoomOf(map.MapInstanceId) != null && EndState == null && _down.Add(player.CharacterId))
                {
                    _pending.Add(() => _host.PlayerDownAsync(this, player));
                }

                return Task.CompletedTask;
            });
        }

        public bool IsDown(long characterId) => _down.Contains(characterId);

        // For a host that brings a fallen player back; only call it from one of the host's hooks.
        public void StoodUp(long characterId) => _down.Remove(characterId);

        public Task TickAsync()
        {
            return RunAsync(async () =>
            {
                var now = _clock.GetCurrentInstant();
                if (EndState == null && _started)
                {
                    _pending.Add(() => _host.TickAsync(this));
                    if (_escorts.Count > 0)
                    {
                        _pending.Add(RecruitEscortsAsync);
                    }
                }

                if (EndState == null && _clockEndsAt is { } endsAt && now >= endsAt)
                {
                    _clockEndsAt = null;
                    await CallScriptAsync(run => _script.OnTimeout(run));
                    if (EndState == null)
                    {
                        End(EndStateType.Timeout);
                    }
                }

                while (EndState == null && !_abandoned && NextDueStep(now) is { } step)
                {
                    await CallScriptAsync(step);
                }

                if (_returnAt is { } returnAt && now >= returnAt)
                {
                    _returnAt = null;
                    _pending.Add(ReturnEveryoneAsync);
                }
            });
        }

        private Action<IInstanceRun>? NextDueStep(Instant now)
        {
            if (_scriptStartedAt is not { } startedAt)
            {
                return null;
            }

            var index = _scheduled.FindIndex(s => startedAt.Plus(s.At) <= now);
            if (index < 0)
            {
                return null;
            }

            var step = _scheduled[index].Step;
            _scheduled.RemoveAt(index);
            return step;
        }

        public void At(TimeSpan elapsed, Action<IInstanceRun> step)
        {
            var at = Duration.FromTimeSpan(elapsed);
            var index = _scheduled.FindIndex(s => s.At > at);
            _scheduled.Insert(index < 0 ? _scheduled.Count : index, (at, step));
        }

        public void SummonWave(int room, byte wave)
        {
            var mapInstance = _rooms[room];
            foreach (var line in _host.WaveMonsters(wave))
            {
                if (!_npcMonsters.TryGetValue(line.VNum, out var npcMonster))
                {
                    _logger.LogError(_logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_MONSTER_MISSING], Name, line.VNum);
                    continue;
                }

                for (var i = 0; i < line.Amount; i++)
                {
                    var (x, y) = mapInstance.Map.RandomWalkableCell(_random, 100) ?? (0, 0);
                    Counted(room);
                    _pending.Add(() => OnRoom(mapInstance, () =>
                    {
                        var monster = mapInstance.SpawnMonster(npcMonster, x, y, line.IsMoving);
                        return mapInstance.SendPacketAsync(monster.GenerateIn());
                    }));
                }
            }
        }

        public void DropWave(int room, byte wave)
        {
            var mapInstance = _rooms[room];
            _pending.Add(() => OnRoom(mapInstance, () => _host.DropWaveAsync(mapInstance, wave)));
        }

        public void Throw(int room, Spot around, params ItemPile[] piles)
        {
            var mapInstance = _rooms[room];
            _pending.Add(() => OnRoom(mapInstance, () => _itemThrowService.ThrowAsync(mapInstance, around, piles)));
        }

        public int MonstersAlive(int room) => _aliveByRoom.GetValueOrDefault(room);

        public int PlayersIn(int room) => PlayersOf(_rooms[room]).Count();

        public bool OneSideStanding(int room)
        {
            var standing = PlayersOf(_rooms[room]).Where(p => !_down.Contains(p.CharacterId)).ToList();
            return standing.All(p => standing.All(o => o.CharacterId == p.CharacterId
                || p.Group.ContainsKey(new Tuple<VisualType, long>(VisualType.Player, o.VisualId))));
        }

        public void SetPvp(int room, bool isPvp)
        {
            var mapInstance = _rooms[room];
            _pending.Add(() => OnRoom(mapInstance, () =>
            {
                mapInstance.IsPvp = isPvp;
                return Task.CompletedTask;
            }));
        }

        private IEnumerable<PlayerComponentBundle> PlayersOf(MapInstance room) =>
            _sessionRegistry.GetClientSessionsByMapInstance(room.MapInstanceId)
                .Where(s => s.HasPlayerEntity)
                .Select(s => s.Character);

        private void Counted(int room)
        {
            MonstersSummoned++;
            _aliveByRoom[room] = _aliveByRoom.GetValueOrDefault(room) + 1;
        }

        public void Summon(int room, params MonsterSpawn[] monsters)
        {
            var mapInstance = _rooms[room];
            foreach (var spawn in monsters)
            {
                if (!_npcMonsters.TryGetValue(spawn.VNum, out var npcMonster))
                {
                    _logger.LogError(_logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_MONSTER_MISSING], Name, spawn.VNum);
                    continue;
                }

                Counted(room);
                _pending.Add(() => OnRoom(mapInstance, () =>
                {
                    var monster = mapInstance.SummonMonster(npcMonster, spawn.At.X, spawn.At.Y, spawn.IsHostile);
                    monster.IsBonus = spawn.IsBonus;
                    monster.DeathStep = spawn.OnDeath;
                    if (spawn.IsBonus)
                    {
                        monster.Effect = BonusEffect;
                        monster.EffectDelay = BonusEffectDelay;
                    }

                    return mapInstance.SendPacketAsync(monster.GenerateIn());
                }));
            }
        }

        public void SummonNpcs(int room, params NpcSpawn[] npcs)
        {
            var mapInstance = _rooms[room];
            foreach (var spawn in npcs)
            {
                if (!_npcMonsters.TryGetValue(spawn.VNum, out var npcMonster))
                {
                    _logger.LogError(_logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_MONSTER_MISSING], Name, spawn.VNum);
                    continue;
                }

                NpcsSummoned++;
                _pending.Add(() => OnRoom(mapInstance, () =>
                {
                    var npc = mapInstance.SummonNpc(npcMonster, spawn.At.X, spawn.At.Y);
                    if (spawn.IsEscort)
                    {
                        npc.IsEscort = true;
                        _escorts.Add((mapInstance, (int)npc.VisualId));
                    }

                    return mapInstance.SendPacketAsync(npc.GenerateIn());
                }));
            }
        }

        private async Task RecruitEscortsAsync()
        {
            foreach (var (room, npcId) in _escorts)
            {
                if (room.GetNpcById(npcId) is not { IsAlive: true, LeaderId: 0 } escort)
                {
                    continue;
                }

                if (PlayersOf(room)
                        .Where(p => p.IsAlive && Math.Max(Math.Abs(p.PositionX - escort.PositionX),
                            Math.Abs(p.PositionY - escort.PositionY)) <= EscortReach)
                        .Select(p => (PlayerComponentBundle?)p)
                        .FirstOrDefault() is not { } leader)
                {
                    continue;
                }

                escort.LeaderId = leader.CharacterId;
                await room.SendPacketAsync(escort.GenerateEff(EscortJoinedEffect));
                await leader.SendPacketAsync(new CtlPacket
                {
                    Type = VisualType.Npc, PetId = escort.VisualId, Action = EscortJoinedAction, Unknown = 0
                });
                await room.SendPacketAsync(escort.GenerateCond());
            }
        }

        // A led escort is carried over beside its leader rather than walking, as the rooms are separate maps.
        private void BringEscorts(PlayerComponentBundle leader, int room)
        {
            var arrival = _rooms[room];
            for (var i = 0; i < _escorts.Count; i++)
            {
                var index = i;
                var (left, npcId) = _escorts[index];
                if (left == arrival)
                {
                    continue;
                }

                _pending.Add(() => OnRoom(left, async () =>
                {
                    if (left.GetNpcById(npcId) is not { IsAlive: true } npc || npc.LeaderId != leader.CharacterId)
                    {
                        return;
                    }

                    var (hp, mp, npcMonster) = (npc.Hp, npc.Mp, npc.NpcMonster);
                    left.RemoveNpc(npc);
                    await left.SendPacketAsync(new OutPacket { VisualType = VisualType.Npc, VisualId = npcId });
                    _pending.Add(() => OnRoom(arrival, () =>
                    {
                        var moved = arrival.SummonNpc(npcMonster, leader.PositionX, leader.PositionY);
                        moved.Hp = hp;
                        moved.Mp = mp;
                        moved.IsEscort = true;
                        moved.LeaderId = leader.CharacterId;
                        _escorts[index] = (arrival, (int)moved.VisualId);
                        return arrival.SendPacketAsync(moved.GenerateIn());
                    }));
                }));
            }
        }

        public Task EscortFellAsync(Guid mapInstanceId)
        {
            return RunAsync(() =>
            {
                if (RoomOf(mapInstanceId) != null && EndState == null && !_abandoned)
                {
                    End(EndStateType.Fail);
                }

                return Task.CompletedTask;
            });
        }

        public void Hunt(int room, short vNum)
        {
            if (!_npcMonsters.TryGetValue(vNum, out var npcMonster))
            {
                _logger.LogError(_logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_MONSTER_MISSING], Name, vNum);
                return;
            }

            var mapInstance = _rooms[room];
            var preyId = _newestArrival.TryGetValue(room, out var characterId) ? characterId : (long?)null;
            _pending.Add(() => OnRoom(mapInstance, () =>
            {
                var prey = PlayersOf(mapInstance).FirstOrDefault(p => p.CharacterId == preyId && p.IsAlive);
                var (x, y) = prey is { } target
                    ? (target.PositionX, target.PositionY)
                    : mapInstance.Map.RandomWalkableCell(_random, 100) ?? ((short)0, (short)0);
                var monster = mapInstance.SpawnMonster(npcMonster, x, y, true);
                if (prey is { } hunted)
                {
                    _aggroService.AddThreat(monster, hunted, 1);
                }

                return mapInstance.SendPacketAsync(monster.GenerateIn());
            }));
        }

        public void Dismiss(int room, short vNum)
        {
            var mapInstance = _rooms[room];
            _pending.Add(() => OnRoom(mapInstance, async () =>
            {
                foreach (var monster in mapInstance.EnumerateMonsters().Where(m => m.VNum == vNum && m.IsAlive).ToList())
                {
                    monster.IsAlive = false;
                    _aggroService.Clear(monster);
                    await mapInstance.SendPacketAsync(new OutPacket { VisualType = VisualType.Monster, VisualId = monster.VisualId });
                }
            }));
        }

        public void SetRates(int room, int experience, int drops)
        {
            var mapInstance = _rooms[room];
            _pending.Add(() => OnRoom(mapInstance, () =>
            {
                mapInstance.XpRate = experience;
                mapInstance.DropRate = drops;
                return Task.CompletedTask;
            }));
        }

        public void SpawnPortal(int fromRoom, Spot at, int toRoom, Spot arrival, bool locked = true)
        {
            var from = _rooms[fromRoom];
            var to = _rooms[toRoom];
            var portal = new Portal
            {
                PortalId = ++_nextPortalId,
                SourceMapId = from.Map.MapId,
                SourceX = at.X,
                SourceY = at.Y,
                SourceMapInstanceId = from.MapInstanceId,
                DestinationMapId = to.Map.MapId,
                DestinationX = arrival.X,
                DestinationY = arrival.Y,
                DestinationMapInstanceId = to.MapInstanceId,
                Type = locked ? PortalType.Closed : PortalType.Open
            };
            _pending.Add(() => OnRoom(from, () =>
            {
                from.Portals.Add(portal);
                return from.SendPacketAsync(portal.GenerateGp());
            }));
        }

        public void UnlockPortal(int fromRoom, int toRoom)
        {
            var from = _rooms[fromRoom];
            var to = _rooms[toRoom].MapInstanceId;
            _pending.Add(() => OnRoom(from, async () =>
            {
                foreach (var portal in from.Portals.Where(s => s.DestinationMapInstanceId == to).ToList())
                {
                    portal.Type = PortalType.Open;
                    await from.SendPacketAsync(portal.GenerateGp());
                }
            }));
        }

        public void SpawnExit(int room, Spot at, bool locked = true)
        {
            var mapInstance = _rooms[room];
            var portal = new Portal
            {
                PortalId = ++_nextPortalId,
                SourceMapId = mapInstance.Map.MapId,
                SourceX = at.X,
                SourceY = at.Y,
                SourceMapInstanceId = mapInstance.MapInstanceId,
                DestinationMapId = mapInstance.Map.MapId,
                Type = locked ? PortalType.TsEndClosed : PortalType.TsEnd
            };
            _pending.Add(() => OnRoom(mapInstance, () =>
            {
                mapInstance.Portals.Add(portal);
                return mapInstance.SendPacketAsync(portal.GenerateGp());
            }));
        }

        public void UnlockExit(int room)
        {
            var mapInstance = _rooms[room];
            _pending.Add(() => OnRoom(mapInstance, async () =>
            {
                foreach (var portal in mapInstance.Portals.Where(s => s.Type == PortalType.TsEndClosed).ToList())
                {
                    portal.Type = PortalType.TsEnd;
                    await mapInstance.SendPacketAsync(portal.GenerateGp());
                }
            }));
        }

        public void SetMonsterLocker(int room, short count)
        {
            _lockers[room] = count;
            _lockerVNums.Remove(room);
        }

        public void SetMonsterLocker(int room, short count, short countedVNum)
        {
            _lockers[room] = count;
            _lockerVNums[room] = countedVNum;
        }

        public int LockerLeft(int room) => _lockers.GetValueOrDefault(room);

        public bool IsButtonOn(int room, int button) =>
            _rooms[room].Buttons.TryGetValue(button, out var placed) && placed.IsOn;

        public void PlaceButton(int room, int button, Spot at, short offVNum, short onVNum, bool toggles = false, ButtonKind kind = ButtonKind.Lever)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(button, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(button, MapButton.LastId);
            var mapInstance = _rooms[room];
            var placed = new MapButton
            {
                MapButtonId = button,
                PositionX = at.X,
                PositionY = at.Y,
                OffVNum = offVNum,
                OnVNum = onVNum,
                Toggles = toggles,
                Kind = kind
            };
            _pending.Add(() => OnRoom(mapInstance, () =>
            {
                mapInstance.Buttons[button] = placed;
                return mapInstance.SendPacketAsync(placed.GenerateIn());
            }));
        }

        public void RemoveButton(int room, int button)
        {
            var mapInstance = _rooms[room];
            _pending.Add(() => OnRoom(mapInstance, () =>
                mapInstance.Buttons.TryRemove(button, out var removed)
                    ? mapInstance.SendPacketAsync(removed.GenerateOut())
                    : Task.CompletedTask));
        }

        public void PlaceNpc(int room, short vNum, Spot at)
        {
            if (!_npcMonsters.TryGetValue(vNum, out var npcMonster))
            {
                _logger.LogError(_logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_MONSTER_MISSING], Name, vNum);
                return;
            }

            var mapInstance = _rooms[room];
            _pending.Add(() => OnRoom(mapInstance, async () =>
            {
                var npc = mapInstance.PlaceNpc(npcMonster, at.X, at.Y);
                await mapInstance.SendPacketAsync(npc.GenerateIn());
                await _host.NpcPlacedAsync(this, mapInstance, npc);
            }));
        }

        public Task UseButtonAsync(Guid mapInstanceId, int button, long userId)
        {
            return RunAsync(async () =>
            {
                var room = RoomOf(mapInstanceId);
                if (room == null || EndState != null || _abandoned
                    || !_rooms[room.Value].Buttons.TryGetValue(button, out var used)
                    || (used.IsOn && !used.Toggles))
                {
                    return;
                }

                used.IsOn = !used.IsOn;
                if (used.IsOn)
                {
                    Advance(ObjectiveKind.UseObject, used.OffVNum);
                }

                var mapInstance = _rooms[room.Value];
                _pending.Add(() => OnRoom(mapInstance, async () =>
                {
                    if (used.Kind == ButtonKind.Key)
                    {
                        mapInstance.Buttons.TryRemove(button, out _);
                        await mapInstance.SendPacketAsync(new ServerGetPacket { VisualType = VisualType.Player, VisualId = userId, ItemId = button });
                        return;
                    }

                    await mapInstance.SendPacketAsync(used.GenerateOut());
                    await mapInstance.SendPacketAsync(used.GenerateIn());
                    if (used.Kind == ButtonKind.Sculpture)
                    {
                        return;
                    }

                    await mapInstance.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.LeverHasBeenPulled });
                }));
                await CallScriptAsync(run => _script.OnButtonUsed(run, room.Value, button));
            });
        }

        public void StartClock(TimeSpan limit)
        {
            _clockLimit = Duration.FromTimeSpan(limit);
            var endsAt = _clock.GetCurrentInstant().Plus(_clockLimit);
            _clockEndsAt = endsAt;
            _pending.Add(() => BroadcastAsync(GenerateClock(endsAt)));
        }

        public void AddClockTime(TimeSpan extra)
        {
            if (_clockEndsAt is not { } endsAt)
            {
                return;
            }

            var added = Duration.FromTimeSpan(extra);
            _clockLimit += added;
            _clockEndsAt = endsAt.Plus(added);
            var newEnd = _clockEndsAt.Value;
            _pending.Add(() => BroadcastAsync(GenerateClock(newEnd)));
        }

        public void StartRoomClock(int room, TimeSpan length)
        {
            var mapInstance = _rooms[room];
            var deciseconds = (int)(length.TotalMilliseconds / 100);
            _pending.Add(() => mapInstance.SendPacketAsync(new EventPacket
            {
                Type = EventPacketType.RedTimer,
                Parameter = deciseconds,
                SecondParameter = deciseconds
            }));
        }

        public void StopRoomClock(int room)
        {
            var mapInstance = _rooms[room];
            _pending.Add(() => mapInstance.SendPacketAsync(new EventPacket
            {
                Type = EventPacketType.RedTimer,
                IsDisabled = true,
                Parameter = -1,
                SecondParameter = -1
            }));
        }

        public void Dialog(int room, int dialog)
        {
            var mapInstance = _rooms[room];
            _pending.Add(async () =>
            {
                foreach (var player in PlayersOf(mapInstance).ToList())
                {
                    await player.SendPacketAsync(new NpcReqPacket
                    {
                        Type = VisualType.Player,
                        TargetId = player.CharacterId,
                        Data = dialog
                    });
                }
            });
        }

        public void ShowCountdown(int room, TimeSpan length)
        {
            var mapInstance = _rooms[room];
            _pending.Add(() => _host.CountdownAsync(this, mapInstance, length));
        }

        public bool CallFighters(int room) => _host.CallFighters(this, _rooms[room]);

        public void OpenBout(int room) => _host.OpenBout(this, _rooms[room]);

        public int FightersStanding(int room) => _host.FightersStanding(this, _rooms[room]);

        public void EndBout(int room) => _host.EndBout(this, _rooms[room]);

        // Work a host starts from a script verb; it runs once the script's step has returned.
        public void Queue(Func<Task> work) => _pending.Add(work);

        public void Message(Game18NConstString message)
        {
            _pending.Add(() => BroadcastAsync(new MsgiPacket { Type = MessageType.Default, Message = message }));
        }

        public void Message(Game18NConstString message, long value)
        {
            _pending.Add(() => BroadcastAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = message,
                ArgumentType = 4,
                Game18NArguments = { value }
            }));
        }

        public void Succeed() => End(EndStateType.Success);

        // Each player opens the reward chest of a successful run once.
        public bool TryOpenChest(long characterId) =>
            EndState == EndStateType.Success && _chestsOpened.TryAdd(characterId, 0);

        public void Fail() => End(EndStateType.Fail);

        public void RunOutOfLives() => End(EndStateType.LivesExhausted);

        private void End(EndStateType state)
        {
            if (EndState != null)
            {
                return;
            }

            EndState = state;
            _clockEndsAt = null;
            _scheduled.Clear();
            _returnAt = _clock.GetCurrentInstant().Plus(ReturnDelay);
            foreach (var room in _rooms.Values)
            {
                room.IsPvp = false;
            }

            _pending.Add(() =>
            {
                _endingApplied = true;
                return _host.EndedAsync(this, state);
            });
        }

        private EventPacket GenerateClock(Instant endsAt)
        {
            var remaining = Math.Max(0, (endsAt - _clock.GetCurrentInstant()).TotalMilliseconds);
            return new EventPacket
            {
                Type = EventPacketType.TSTimer,
                Parameter = (int)(remaining / 100),
                SecondParameter = (int)(_clockLimit.TotalMilliseconds / 100)
            };
        }

        private int? RoomOf(Guid mapInstanceId)
        {
            foreach (var (key, room) in _rooms)
            {
                if (room.MapInstanceId == mapInstanceId)
                {
                    return key;
                }
            }

            return null;
        }

        public IEnumerable<ClientSession> Players() =>
            _rooms.Values.SelectMany(room => _sessionRegistry.GetClientSessionsByMapInstance(room.MapInstanceId));

        public async Task BroadcastAsync(IPacket packet)
        {
            foreach (var room in _rooms.Values)
            {
                await room.SendPacketAsync(packet);
            }
        }

        private async Task ReturnEveryoneAsync()
        {
            foreach (var session in Players().ToList())
            {
                await _host.ReturnAsync(session);
            }
        }

        // A script that throws or never returns is abandoned: a stuck script cannot be stopped,
        // so its verbs are shut off, the run fails, and everyone is sent back as after any
        // other ending.
        private void Abandon()
        {
            _abandoned = true;
            if (!_endingApplied)
            {
                // The ending was asked for but never reached the players, so the failure replaces it.
                EndState = null;
            }

            End(EndStateType.Fail);
        }

        private async Task CallScriptAsync(Action<IInstanceRun> call)
        {
            var scope = new TriggerScope(this);
            try
            {
                await Task.Run(() => call(scope)).WaitAsync(_stepTimeout);
            }
            catch (TimeoutException)
            {
                scope.Close();
                _logger.LogError(_logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_SCRIPT_TIMED_OUT], Name, _stepTimeout);
                throw new ScriptAbandonedException();
            }
            catch (Exception exception)
            {
                scope.Close();
                _logger.LogError(exception, _logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_SCRIPT_FAILED], Name);
                throw new ScriptAbandonedException();
            }
        }

        // Work left on a room's queue by an abandoned run must not land after the failure.
        private Task OnRoom(MapInstance room, Func<Task> action) =>
            room.Executor.RunAsync(() => _abandoned ? Task.CompletedTask : action());

        // Triggers and the work they ask for run one at a time and in order. The work is
        // carried out once the script has returned, so a script never sees the run half-applied.
        private async Task RunAsync(Func<Task> trigger)
        {
            await _gate.WaitAsync();
            try
            {
                try
                {
                    await trigger();
                }
                catch (ScriptAbandonedException)
                {
                    _pending.Clear();
                    Abandon();
                }
                catch (Exception exception)
                {
                    _pending.Clear();
                    _logger.LogError(exception, _logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_FAILED], Name);
                    return;
                }

                await ApplyPendingAsync();
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task ApplyPendingAsync()
        {
            while (_pending.Count > 0)
            {
                var step = _pending[0];
                _pending.RemoveAt(0);
                try
                {
                    await step().WaitAsync(_stepTimeout);
                }
                catch (TimeoutException)
                {
                    _logger.LogError(_logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_STEP_TIMED_OUT], Name, _stepTimeout);
                    _pending.Clear();
                    if (!_abandoned)
                    {
                        Abandon();
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, _logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_STEP_FAILED], Name);
                }
            }
        }

        private sealed class ScriptAbandonedException : Exception;

        // What a script sees during one trigger. Once that trigger is abandoned, a script still
        // running in the background can no longer reach the run.
        private sealed class TriggerScope(ScriptedInstanceRun run) : IInstanceRun
        {
            private readonly Lock _lock = new();
            private bool _open = true;

            public TimeSpan Elapsed => run.Elapsed;

            public void Close()
            {
                lock (_lock)
                {
                    _open = false;
                }
            }

            public void At(TimeSpan elapsed, Action<IInstanceRun> step) => Do(() => run.At(elapsed, step));

            public void Summon(int room, params MonsterSpawn[] monsters) => Do(() => run.Summon(room, monsters));

            public void SummonWave(int room, byte wave) => Do(() => run.SummonWave(room, wave));

            public void DropWave(int room, byte wave) => Do(() => run.DropWave(room, wave));

            public void Throw(int room, Spot around, params ItemPile[] piles) => Do(() => run.Throw(room, around, piles));

            public void SummonNpcs(int room, params NpcSpawn[] npcs) => Do(() => run.SummonNpcs(room, npcs));

            public int MonstersAlive(int room) => run.MonstersAlive(room);

            public int PlayersIn(int room) => run.PlayersIn(room);

            public bool OneSideStanding(int room) => run.OneSideStanding(room);

            public void SetPvp(int room, bool isPvp) => Do(() => run.SetPvp(room, isPvp));

            public void Hunt(int room, short vNum) => Do(() => run.Hunt(room, vNum));

            public void Dismiss(int room, short vNum) => Do(() => run.Dismiss(room, vNum));

            public void SetRates(int room, int experience, int drops) => Do(() => run.SetRates(room, experience, drops));

            public void SpawnPortal(int fromRoom, Spot at, int toRoom, Spot arrival, bool locked = true) =>
                Do(() => run.SpawnPortal(fromRoom, at, toRoom, arrival, locked));

            public void UnlockPortal(int fromRoom, int toRoom) => Do(() => run.UnlockPortal(fromRoom, toRoom));

            public void SpawnExit(int room, Spot at, bool locked = true) => Do(() => run.SpawnExit(room, at, locked));

            public void UnlockExit(int room) => Do(() => run.UnlockExit(room));

            public void SetMonsterLocker(int room, short count) => Do(() => run.SetMonsterLocker(room, count));

            public void SetMonsterLocker(int room, short count, short countedVNum) =>
                Do(() => run.SetMonsterLocker(room, count, countedVNum));

            public int LockerLeft(int room) => run.LockerLeft(room);

            public bool IsButtonOn(int room, int button) => run.IsButtonOn(room, button);

            public void PlaceButton(int room, int button, Spot at, short offVNum, short onVNum, bool toggles = false, ButtonKind kind = ButtonKind.Lever) =>
                Do(() => run.PlaceButton(room, button, at, offVNum, onVNum, toggles, kind));

            public void RemoveButton(int room, int button) => Do(() => run.RemoveButton(room, button));

            public void PlaceNpc(int room, short vNum, Spot at) => Do(() => run.PlaceNpc(room, vNum, at));

            public void StartClock(TimeSpan limit) => Do(() => run.StartClock(limit));

            public void AddClockTime(TimeSpan extra) => Do(() => run.AddClockTime(extra));

            public void StartRoomClock(int room, TimeSpan length) => Do(() => run.StartRoomClock(room, length));

            public void StopRoomClock(int room) => Do(() => run.StopRoomClock(room));

            public void Dialog(int room, int dialog) => Do(() => run.Dialog(room, dialog));

            public void ShowCountdown(int room, TimeSpan length) => Do(() => run.ShowCountdown(room, length));

            public bool CallFighters(int room)
            {
                var called = false;
                Do(() => called = run.CallFighters(room));
                return called;
            }

            public void OpenBout(int room) => Do(() => run.OpenBout(room));

            public int FightersStanding(int room) => run.FightersStanding(room);

            public void EndBout(int room) => Do(() => run.EndBout(room));

            public void Message(Game18NConstString message) => Do(() => run.Message(message));

            public void Message(Game18NConstString message, long value) => Do(() => run.Message(message, value));

            public void Succeed() => Do(run.Succeed);

            public void Fail() => Do(run.Fail);

            private void Do(Action verb)
            {
                lock (_lock)
                {
                    if (_open)
                    {
                        verb();
                    }
                }
            }
        }
    }
}
