//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Infastructure;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NodaTime;
using NosCore.Packets.ServerPackets.Portals;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Mapster;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.MiniMap;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

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
        private readonly IAggroService _aggroService;
        private readonly IDao<MapMonsterDto, int> _mapMonsters;
        private readonly List<NpcMonsterDto> _npcMonsterList;
        private readonly Lazy<IMapChangeService> _mapChangeService;
        private readonly IClock _clock;
        private readonly IRandomProvider _random;

        private readonly ConcurrentDictionary<Guid, ScriptedInstanceRun> _runsByRoom = new();
        private readonly Dictionary<short, MapDto> _mapsByVNum;
        private readonly Dictionary<short, NpcMonsterDto> _npcMonsters;
        private readonly ILogger<ScriptedInstanceService> _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;
        private readonly TimeSpan _scriptTimeout;

        public ScriptedInstanceService(List<ScriptedInstanceDto> scriptedInstances,
            List<MapDto> maps,
            List<NpcMonsterDto> npcMonsters,
            Lazy<IMapInstanceGeneratorService> mapInstanceGeneratorService,
            IMapInstanceRegistry mapInstanceRegistry,
            IInstanceScriptLoader scriptLoader,
            ISessionRegistry sessionRegistry,
            IAggroService aggroService,
            IDao<MapMonsterDto, int> mapMonsters,
            Lazy<IMapChangeService> mapChangeService,
            IClock clock,
            IRandomProvider random,
            IOptions<WorldConfiguration> worldConfiguration,
            ILogger<ScriptedInstanceService> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _scriptTimeout = worldConfiguration.Value.InstanceScriptTimeout;
            _mapInstanceGeneratorService = mapInstanceGeneratorService;
            _mapInstanceRegistry = mapInstanceRegistry;
            _scriptLoader = scriptLoader;
            _sessionRegistry = sessionRegistry;
            _aggroService = aggroService;
            _mapMonsters = mapMonsters;
            _npcMonsterList = npcMonsters;
            _mapChangeService = mapChangeService;
            _clock = clock;
            _random = random;
            _logger = logger;
            _logLanguage = logLanguage;
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
                entrance.Logic = await _scriptLoader.LoadAsync(entrance.Script).WaitAsync(_scriptTimeout);
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, _logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_UNREADABLE], entrance.Script);
                return false;
            }
        }

        public async Task<ScriptedInstance?> GetRaidBySealAsync(short sealVNum)
        {
            foreach (var entrance in _byMap.Values.SelectMany(s => s).Where(s => s.Type == ScriptedInstanceType.Raid))
            {
                if (await LoadAsync(entrance) && entrance.Definition?.RequiredItems.Any(s => s.VNum == sealVNum) == true)
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
            if (!await LoadAsync(entrance) || entrance.Logic is not { } logic)
            {
                return null;
            }

            var instanceType = entrance.Type == ScriptedInstanceType.TimeSpace
                ? MapInstanceType.TimeSpaceInstance
                : MapInstanceType.RaidInstance;

            return await InstantiateAsync(logic, instanceType, new TimeSpaceRunHost(entrance, _mapChangeService));
        }

        public async Task<IInstanceScript?> LoadScriptAsync(string script)
        {
            try
            {
                return await _scriptLoader.LoadAsync(script).WaitAsync(_scriptTimeout);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, _logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_UNREADABLE], script);
                return null;
            }
        }

        public async Task<ScriptedInstanceRun?> InstantiateAsync(IInstanceScript logic, MapInstanceType instanceType,
            IInstanceRunHost host)
        {
            var rooms = logic.Definition.Rooms;
            if (rooms.Count == 0)
            {
                return null;
            }

            var built = new Dictionary<int, MapInstance>();
            foreach (var room in rooms)
            {
                if (!_mapsByVNum.TryGetValue(room.VNum, out var map))
                {
                    _logger.LogError(
                        _logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_MAP_MISSING],
                        host.Name, room.VNum);
                    await RemoveRoomsAsync(built.Values.Select(s => s.MapInstanceId));
                    return null;
                }

                var id = Guid.NewGuid();
                var mapInstance = _mapInstanceGeneratorService.Value.CreateMapInstance(
                    map.Adapt<Map.Map>(), id, false, instanceType);
                mapInstance.MapIndexX = room.IndexX;
                mapInstance.MapIndexY = room.IndexY;
                if (room.KeepsMapMonsters)
                {
                    mapInstance.LoadMonsters(_mapMonsters.Where(m => m.MapId == room.VNum)?.ToList() ?? [], _npcMonsterList);
                }

                await _mapInstanceGeneratorService.Value.AddMapInstanceAsync(mapInstance);
                await mapInstance.StartLifeAsync();
                built[room.Key] = mapInstance;
            }

            var run = new ScriptedInstanceRun(host, logic, built, _npcMonsters, _random, _clock,
                _sessionRegistry, _aggroService, _logger, _logLanguage, _scriptTimeout);
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
                    _logger.LogError(exception, _logLanguage[LogLanguageKey.SCRIPTED_INSTANCE_TICK_FAILED], run.Name);
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

        // Only raids with a script get a gate; the rest would have nothing to start.
        public IEnumerable<GpPacket> GenerateRaidGates(short mapId)
        {
            return GetByMap(mapId)
                .Where(s => s.Type == ScriptedInstanceType.Raid && !string.IsNullOrWhiteSpace(s.Script))
                .Select(s => new GpPacket
                {
                    SourceX = s.PositionX,
                    SourceY = s.PositionY,
                    MapId = mapId,
                    PortalType = PortalType.Raid,
                    PortalId = s.ScriptedInstanceId
                });
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
