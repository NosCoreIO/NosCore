//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Infastructure;
using NosCore.Data.StaticEntities;
using NodaTime;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Mapster;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.MiniMap;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public class ScriptedInstanceService : IScriptedInstanceService, ISingletonService
    {
        private static readonly IReadOnlyList<ScriptedInstance> None = [];

        private readonly Dictionary<short, List<ScriptedInstance>> _byMap;

        private readonly Lazy<IMapInstanceGeneratorService> _mapInstanceGeneratorService;
        private readonly IMapInstanceRegistry _mapInstanceRegistry;
        private readonly IInstanceScriptLoader _scriptLoader;
        private readonly ISessionRegistry _sessionRegistry;
        private readonly Lazy<IMapChangeService> _mapChangeService;
        private readonly IClock _clock;

        private readonly ConcurrentDictionary<Guid, ScriptedInstanceRun> _runsByRoom = new();
        private readonly Dictionary<short, MapDto> _mapsByVNum;
        private readonly Dictionary<short, NpcMonsterDto> _npcMonsters;
        private readonly ILogger<ScriptedInstanceService> _logger;

        public ScriptedInstanceService(List<ScriptedInstanceDto> scriptedInstances,
            List<MapDto> maps,
            List<NpcMonsterDto> npcMonsters,
            Lazy<IMapInstanceGeneratorService> mapInstanceGeneratorService,
            IMapInstanceRegistry mapInstanceRegistry,
            IInstanceScriptLoader scriptLoader,
            ISessionRegistry sessionRegistry,
            Lazy<IMapChangeService> mapChangeService,
            IClock clock,
            ILogger<ScriptedInstanceService> logger)
        {
            _mapInstanceGeneratorService = mapInstanceGeneratorService;
            _mapInstanceRegistry = mapInstanceRegistry;
            _scriptLoader = scriptLoader;
            _sessionRegistry = sessionRegistry;
            _mapChangeService = mapChangeService;
            _clock = clock;
            _logger = logger;
            _mapsByVNum = maps.GroupBy(s => s.MapId).ToDictionary(s => s.Key, s => s.First());
            _npcMonsters = npcMonsters.GroupBy(s => s.NpcMonsterVNum).ToDictionary(s => s.Key, s => s.First());

            _byMap = scriptedInstances
                .Select(row => row.Adapt<ScriptedInstance>())
                .GroupBy(s => s.MapId)
                .ToDictionary(s => s.Key, s => s.OrderBy(o => o.PositionY).ThenBy(o => o.PositionX).ToList());
        }

        public async Task<bool> LoadAsync(ScriptedInstance entrance)
        {
            if (entrance.Logic != null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(entrance.Script))
            {
                return false;
            }

            try
            {
                entrance.Logic = await _scriptLoader.LoadAsync(entrance.Script);
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "The script {Script} of ScriptedInstance {ScriptedInstanceId} on map {MapId} could not be loaded",
                    entrance.Script, entrance.ScriptedInstanceId, entrance.MapId);
                return false;
            }
        }

        public async Task<ScriptedInstance?> GetRaidBySealAsync(short sealVNum)
        {
            foreach (var entrance in _byMap.Values.SelectMany(s => s).Where(s => s.Type == ScriptedInstanceType.Raid))
            {
                if (await LoadAsync(entrance) && entrance.Definition!.RequiredItems.Any(s => s.VNum == sealVNum))
                {
                    return entrance;
                }
            }

            return null;
        }

        public IReadOnlyList<ScriptedInstance> GetByMap(short mapId)
        {
            return _byMap.TryGetValue(mapId, out var entrances) ? entrances : None;
        }

        public ScriptedInstance? GetAt(short mapId, short positionX, short positionY)
        {
            return GetByMap(mapId)
                .FirstOrDefault(s => s.PositionX == positionX && s.PositionY == positionY);
        }

        public ScriptedInstance? GetNear(short mapId, short positionX, short positionY)
        {
            return GetByMap(mapId)
                .Where(s => Math.Abs(s.PositionX - positionX) <= 1 && Math.Abs(s.PositionY - positionY) <= 1)
                .OrderBy(s => Math.Abs(s.PositionX - positionX) + Math.Abs(s.PositionY - positionY))
                .FirstOrDefault();
        }

        public async Task<ScriptedInstanceRun?> InstantiateAsync(ScriptedInstance entrance)
        {
            if (!await LoadAsync(entrance))
            {
                return null;
            }

            var rooms = entrance.Logic!.Definition.Rooms;
            if (rooms.Count == 0)
            {
                return null;
            }

            var instanceType = entrance.Type == ScriptedInstanceType.TimeSpace
                ? MapInstanceType.TimeSpaceInstance
                : MapInstanceType.RaidInstance;

            var built = new Dictionary<int, MapInstance>();
            foreach (var room in rooms)
            {
                if (!_mapsByVNum.TryGetValue(room.VNum, out var map))
                {
                    _logger.LogError(
                        "Instance {ScriptedInstanceId} asks for map {VNum}, which does not exist",
                        entrance.ScriptedInstanceId, room.VNum);
                    await RemoveRoomsAsync(built.Values.Select(s => s.MapInstanceId));
                    return null;
                }

                var id = Guid.NewGuid();
                var mapInstance = _mapInstanceGeneratorService.Value.CreateMapInstance(
                    map.Adapt<Map.Map>(), id, false, instanceType);
                mapInstance.MapIndexX = room.IndexX;
                mapInstance.MapIndexY = room.IndexY;

                await _mapInstanceGeneratorService.Value.AddMapInstanceAsync(mapInstance);
                await mapInstance.StartLifeAsync();
                built[room.Key] = mapInstance;
            }

            var run = new ScriptedInstanceRun(entrance, entrance.Logic, built, _npcMonsters, _clock,
                _sessionRegistry, _mapChangeService, _logger);
            foreach (var id in run.Rooms.Values)
            {
                _runsByRoom[id] = run;
            }

            return run;
        }

        public async Task TickAsync()
        {
            foreach (var run in _runsByRoom.Values.Distinct().ToList())
            {
                try
                {
                    await run.TickAsync();
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "The tick of instance {ScriptedInstanceId} failed",
                        run.Entrance.ScriptedInstanceId);
                }
            }
        }

        public ScriptedInstanceRun? GetRun(Guid mapInstanceId)
        {
            return _runsByRoom.TryGetValue(mapInstanceId, out var run) ? run : null;
        }

        public async Task<bool> DisposeIfEmptyAsync(ScriptedInstanceRun run)
        {
            foreach (var id in run.Rooms.Values)
            {
                if (_mapInstanceRegistry.GetById(id)?.Sessions.Count > 0)
                {
                    return false;
                }
            }

            // Removing a room sends anyone who walked in since the count above back out, so
            // the count may be stale; what must not happen is two callers tearing down one run.
            if (!run.TryClose())
            {
                return false;
            }

            await RemoveRoomsAsync(run.Rooms.Values);
            return true;
        }

        private async Task RemoveRoomsAsync(IEnumerable<Guid> rooms)
        {
            foreach (var room in rooms)
            {
                _runsByRoom.TryRemove(room, out _);
                await _mapInstanceGeneratorService.Value.RemoveMapAsync(room);
            }
        }

        public IEnumerable<WpPacket> GenerateWp(short mapId)
        {
            return GetByMap(mapId)
                .Where(s => s.Type == ScriptedInstanceType.TimeSpace)
                .Select(s => new WpPacket
                {
                    PositionX = s.PositionX,
                    PositionY = s.PositionY,
                    ScriptedInstanceId = s.ScriptedInstanceId,
                    PortalType = s.IsHeroic ? WpPortalType.HeroTs : WpPortalType.NormalTs,
                    LevelMinimum = s.LevelMinimum,
                    LevelMaximum = s.LevelMaximum
                });
        }
    }
}
