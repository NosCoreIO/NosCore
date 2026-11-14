//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Instances.Abstractions;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public class ScriptedInstanceRun : IInstanceRun
    {
        private static readonly Duration ReturnDelay = Duration.FromSeconds(10);

        private readonly IInstanceScript _script;
        private readonly IReadOnlyDictionary<int, MapInstance> _rooms;
        private readonly IReadOnlyDictionary<short, NpcMonsterDto> _npcMonsters;
        private readonly IClock _clock;
        private readonly ISessionRegistry _sessionRegistry;
        private readonly Lazy<IMapChangeService> _mapChangeService;
        private readonly ILogger _logger;

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly List<Func<Task>> _pending = [];
        private readonly Dictionary<int, short> _lockers = [];
        private readonly HashSet<int> _visited = [];
        private int _summoned;
        private int _killed;
        private int _nextPortalId;
        private bool _started;
        private Instant? _clockEndsAt;
        private Duration _clockLimit;
        private Instant? _returnAt;
        private readonly TimeSpan _stepTimeout;
        private bool _abandoned;

        public ScriptedInstanceRun(ScriptedInstance entrance, IInstanceScript script,
            IReadOnlyDictionary<int, MapInstance> rooms, IReadOnlyDictionary<short, NpcMonsterDto> npcMonsters,
            IClock clock, ISessionRegistry sessionRegistry, Lazy<IMapChangeService> mapChangeService, ILogger logger,
            TimeSpan stepTimeout)
        {
            _stepTimeout = stepTimeout;
            Entrance = entrance;
            _script = script;
            _rooms = rooms;
            _npcMonsters = npcMonsters;
            _clock = clock;
            _sessionRegistry = sessionRegistry;
            _mapChangeService = mapChangeService;
            _logger = logger;
            Rooms = rooms.ToDictionary(s => s.Key, s => s.Value.MapInstanceId);
            StartedAt = clock.GetCurrentInstant();
        }

        public ScriptedInstance Entrance { get; }

        public IReadOnlyDictionary<int, Guid> Rooms { get; }

        private int _closed;

        // True for exactly one caller: the one that gets to tear the rooms down.
        public bool TryClose() => Interlocked.Exchange(ref _closed, 1) == 0;

        public Instant StartedAt { get; }

        public short ReturnMapId => Entrance.MapId;

        public short ReturnX => Entrance.PositionX;

        public short ReturnY => Entrance.PositionY;

        public EndStateType? EndState { get; private set; }

        public TimeSpan Elapsed => (_clock.GetCurrentInstant() - StartedAt).ToTimeSpan();

        public Task StartAsync()
        {
            return RunAsync(async () =>
            {
                if (_started || _abandoned)
                {
                    return;
                }

                _started = true;
                await CallScriptAsync(run => _script.OnFirstEnter(run));
            });
        }

        public Task MonsterKilledAsync(Guid mapInstanceId, short vNum)
        {
            return RunAsync(async () =>
            {
                var room = RoomOf(mapInstanceId);
                if (room == null)
                {
                    return;
                }

                _killed++;
                if (_abandoned)
                {
                    return;
                }

                await CallScriptAsync(run => _script.OnMonsterKilled(run, room.Value, vNum));

                if (!_lockers.TryGetValue(room.Value, out var remaining) || remaining <= 0)
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

        // The script starts once the first player has finished entering, so what it summons
        // reaches a client that is already on the map.
        public Task EnteredAsync(ClientSession session, Guid mapInstanceId)
        {
            return RunAsync(async () =>
            {
                var room = RoomOf(mapInstanceId);
                if (room != null)
                {
                    _visited.Add(room.Value);
                }

                if (!_started && !_abandoned)
                {
                    _started = true;
                    await CallScriptAsync(run => _script.OnFirstEnter(run));
                }

                if (_clockEndsAt is { } endsAt && EndState == null)
                {
                    _pending.Add(() => session.SendPacketAsync(GenerateClock(endsAt)));
                }
            });
        }

        public Task TickAsync()
        {
            return RunAsync(async () =>
            {
                var now = _clock.GetCurrentInstant();
                if (EndState == null && _clockEndsAt is { } endsAt && now >= endsAt)
                {
                    _clockEndsAt = null;
                    await CallScriptAsync(run => _script.OnTimeout(run));
                    if (EndState == null)
                    {
                        End(EndStateType.Timeout);
                    }
                }

                if (_returnAt is { } returnAt && now >= returnAt)
                {
                    _returnAt = null;
                    _pending.Add(ReturnEveryoneAsync);
                }
            });
        }

        public void Summon(int room, params MonsterSpawn[] monsters)
        {
            var mapInstance = _rooms[room];
            foreach (var spawn in monsters)
            {
                if (!_npcMonsters.TryGetValue(spawn.VNum, out var npcMonster))
                {
                    _logger.LogError("Instance {ScriptedInstanceId} summons monster {VNum}, which does not exist",
                        Entrance.ScriptedInstanceId, spawn.VNum);
                    continue;
                }

                _summoned++;
                _pending.Add(() => mapInstance.Executor.RunAsync(() =>
                {
                    var monster = mapInstance.SummonMonster(npcMonster, spawn.At.X, spawn.At.Y, spawn.IsHostile);
                    return mapInstance.SendPacketAsync(monster.GenerateIn());
                }));
            }
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
            _pending.Add(() => from.Executor.RunAsync(() =>
            {
                from.Portals.Add(portal);
                return from.SendPacketAsync(portal.GenerateGp());
            }));
        }

        public void UnlockPortal(int fromRoom, int toRoom)
        {
            var from = _rooms[fromRoom];
            var to = _rooms[toRoom].MapInstanceId;
            _pending.Add(() => from.Executor.RunAsync(async () =>
            {
                foreach (var portal in from.Portals.Where(s => s.DestinationMapInstanceId == to).ToList())
                {
                    portal.Type = PortalType.Open;
                    await from.SendPacketAsync(portal.GenerateGp());
                }
            }));
        }

        public void SetMonsterLocker(int room, short count)
        {
            _lockers[room] = count;
        }

        public void StartClock(TimeSpan limit)
        {
            _clockLimit = Duration.FromTimeSpan(limit);
            var endsAt = _clock.GetCurrentInstant().Plus(_clockLimit);
            _clockEndsAt = endsAt;
            _pending.Add(() => BroadcastAsync(GenerateClock(endsAt)));
        }

        public void Message(Game18NConstString message)
        {
            _pending.Add(() => BroadcastAsync(new MsgiPacket { Type = MessageType.Default, Message = message }));
        }

        public void Succeed() => End(EndStateType.Success);

        public void Fail() => End(EndStateType.Fail);

        private void End(EndStateType state)
        {
            if (EndState != null)
            {
                return;
            }

            EndState = state;
            _clockEndsAt = null;
            _returnAt = _clock.GetCurrentInstant().Plus(ReturnDelay);
            var score = new ScorePacket
            {
                EndState = state,
                MonsterKilled = (short)_killed,
                RoomsVisited = (short)_visited.Count,
                IsAllMonstersKilled = _killed >= _summoned,
                IsNoNpcDead = true
            };
            _pending.Add(() => BroadcastAsync(score));
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

        private IEnumerable<ClientSession> Players() =>
            _rooms.Values.SelectMany(room => _sessionRegistry.GetClientSessionsByMapInstance(room.MapInstanceId));

        private async Task BroadcastAsync(IPacket packet)
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
                await _mapChangeService.Value.ChangeMapAsync(session, ReturnMapId, ReturnX, ReturnY);
            }
        }

        // A script that throws or never returns is abandoned: a stuck script cannot be stopped,
        // so its verbs are shut off, the run fails, and everyone is sent back as after any
        // other ending.
        private void Abandon()
        {
            _abandoned = true;
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
                _logger.LogError("The script of instance {ScriptedInstanceId} did not return within {Timeout}",
                    Entrance.ScriptedInstanceId, _stepTimeout);
                throw new ScriptAbandonedException();
            }
            catch (Exception exception)
            {
                scope.Close();
                _logger.LogError(exception, "The script of instance {ScriptedInstanceId} failed",
                    Entrance.ScriptedInstanceId);
                throw new ScriptAbandonedException();
            }
        }

        // Triggers run one at a time, and what they ask for is carried out once the script
        // has returned, so a script never sees the run half-applied.
        private async Task RunAsync(Func<Task> trigger)
        {
            await _gate.WaitAsync();
            List<Func<Task>> work;
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

                work = [.. _pending];
                _pending.Clear();
            }
            catch (Exception exception)
            {
                _pending.Clear();
                _logger.LogError(exception, "Instance {ScriptedInstanceId} failed",
                    Entrance.ScriptedInstanceId);
                return;
            }
            finally
            {
                _gate.Release();
            }

            foreach (var step in work)
            {
                try
                {
                    await step().WaitAsync(_stepTimeout);
                }
                catch (TimeoutException)
                {
                    _logger.LogError("A step of instance {ScriptedInstanceId} did not finish within {Timeout}",
                        Entrance.ScriptedInstanceId, _stepTimeout);
                    await RunAsync(() =>
                    {
                        Abandon();
                        return Task.CompletedTask;
                    });
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "A step of instance {ScriptedInstanceId} failed",
                        Entrance.ScriptedInstanceId);
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

            public void Summon(int room, params MonsterSpawn[] monsters) => Do(() => run.Summon(room, monsters));

            public void SpawnPortal(int fromRoom, Spot at, int toRoom, Spot arrival, bool locked = true) =>
                Do(() => run.SpawnPortal(fromRoom, at, toRoom, arrival, locked));

            public void UnlockPortal(int fromRoom, int toRoom) => Do(() => run.UnlockPortal(fromRoom, toRoom));

            public void SetMonsterLocker(int room, short count) => Do(() => run.SetMonsterLocker(room, count));

            public void StartClock(TimeSpan limit) => Do(() => run.StartClock(limit));

            public void Message(Game18NConstString message) => Do(() => run.Message(message));

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
