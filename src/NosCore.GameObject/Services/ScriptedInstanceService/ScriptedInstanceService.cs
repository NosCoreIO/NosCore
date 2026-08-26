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
using NosCore.GameObject.Services.MapInstanceGenerationService;
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

        private readonly IMapInstanceGeneratorService _mapInstanceGeneratorService;
        private readonly IMapInstanceRegistry _mapInstanceRegistry;
        private readonly IClock _clock;

        private readonly ConcurrentDictionary<Guid, ScriptedInstanceRun> _runsByRoom = new();
        private readonly Dictionary<short, MapDto> _mapsByVNum;
        private readonly ILogger<ScriptedInstanceService> _logger;

        public ScriptedInstanceService(List<ScriptedInstanceDto> scriptedInstances,
            List<MapDto> maps,
            IMapInstanceGeneratorService mapInstanceGeneratorService,
            IMapInstanceRegistry mapInstanceRegistry,
            IClock clock,
            ILogger<ScriptedInstanceService> logger)
        {
            _mapInstanceGeneratorService = mapInstanceGeneratorService;
            _mapInstanceRegistry = mapInstanceRegistry;
            _clock = clock;
            _logger = logger;
            _mapsByVNum = maps.GroupBy(s => s.MapId).ToDictionary(s => s.Key, s => s.First());

            _byMap = scriptedInstances
                .Select(row => Build(row, logger))
                .GroupBy(s => s.MapId)
                .ToDictionary(s => s.Key, s => s.OrderBy(o => o.PositionY).ThenBy(o => o.PositionX).ToList());
        }

        private static ScriptedInstance Build(ScriptedInstanceDto row, ILogger logger)
        {
            var instance = row.Adapt<ScriptedInstance>();
            try
            {
                instance.Definition = ScriptedInstanceDefinitionParser.Parse(row.Script);
                return instance;
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "The script of ScriptedInstance {ScriptedInstanceId} on map {MapId} could not be read",
                    row.ScriptedInstanceId, row.MapId);
                return instance;
            }
        }

        public void Register(short mapId, short positionX, short positionY,
            ScriptedInstanceDefinition definition)
        {
            var entrance = GetAt(mapId, positionX, positionY);
            if (entrance == null)
            {
                // A door that was never imported: better said than silently unreachable.
                _logger.LogError(
                    "No instance entrance at {MapId} {PositionX},{PositionY} to attach {Label} to",
                    mapId, positionX, positionY, definition.Label);
                return;
            }

            entrance.Definition = definition;
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

        public async Task<ScriptedInstanceRun?> InstantiateAsync(ScriptedInstance entrance)
        {
            var rooms = entrance.Definition?.Rooms;
            if (rooms == null || rooms.Count == 0)
            {
                return null;
            }

            var instanceType = entrance.Type == ScriptedInstanceType.TimeSpace
                ? MapInstanceType.TimeSpaceInstance
                : MapInstanceType.RaidInstance;

            var built = new Dictionary<int, Guid>();
            foreach (var room in rooms)
            {
                if (!_mapsByVNum.TryGetValue(room.VNum, out var map))
                {
                    _logger.LogError(
                        "Instance {ScriptedInstanceId} asks for map {VNum}, which does not exist",
                        entrance.ScriptedInstanceId, room.VNum);
                    await RemoveRoomsAsync(built.Values).ConfigureAwait(false);
                    return null;
                }

                var id = Guid.NewGuid();
                var mapInstance = _mapInstanceGeneratorService.CreateMapInstance(
                    map.Adapt<Map.Map>(), id, false, instanceType);
                mapInstance.MapIndexX = room.IndexX;
                mapInstance.MapIndexY = room.IndexY;

                await _mapInstanceGeneratorService.AddMapInstanceAsync(mapInstance).ConfigureAwait(false);
                await mapInstance.StartLifeAsync().ConfigureAwait(false);
                built[room.Key] = id;
            }

            var run = new ScriptedInstanceRun(entrance, built, _clock.GetCurrentInstant());
            foreach (var id in built.Values)
            {
                _runsByRoom[id] = run;
            }

            return run;
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

            await RemoveRoomsAsync(run.Rooms.Values).ConfigureAwait(false);
            return true;
        }

        private async Task RemoveRoomsAsync(IEnumerable<Guid> rooms)
        {
            foreach (var room in rooms)
            {
                _runsByRoom.TryRemove(room, out _);
                await _mapInstanceGeneratorService.RemoveMapAsync(room).ConfigureAwait(false);
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
