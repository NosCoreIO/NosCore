//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.GameObject.Providers.MapMonsterProvider;
using NosCore.GameObject.Providers.MapNpcProvider;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Providers.MapInstanceProvider
{
    public class MapInstanceProvider : IMapInstanceProvider
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private readonly ConcurrentDictionary<Guid, MapInstance> MapInstances =
            new ConcurrentDictionary<Guid, MapInstance>();

        public MapInstanceProvider(List<NpcMonsterDto> npcMonsters, List<Map.Map> maps,
            IMapItemProvider mapItemProvider, IMapNpcProvider mapNpcProvider,
            IMapMonsterProvider mapMonsterProvider, IGenericDao<PortalDto> portalDao)
        {
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LOADING_MAPINSTANCES));
            var mapPartitioner = Partitioner.Create(maps, EnumerablePartitionerOptions.NoBuffering);
            var mapList = new ConcurrentDictionary<short, Map.Map>();
            var npccount = 0;
            var monstercount = 0;
            Parallel.ForEach(mapPartitioner, new ParallelOptions {MaxDegreeOfParallelism = 8}, map =>
            {
                var guid = Guid.NewGuid();
                mapList[map.MapId] = map;
                var newMap = new MapInstance(map, guid, map.ShopAllowed, MapInstanceType.BaseMapInstance, npcMonsters,
                    mapItemProvider, mapNpcProvider, mapMonsterProvider);
                MapInstances.TryAdd(guid, newMap);
                newMap.LoadMonsters();
                newMap.LoadNpcs();
                newMap.StartLife();
                monstercount += newMap.Monsters.Count;
                npccount += newMap.Npcs.Count;
            });
            var mapInstancePartitioner =
                Partitioner.Create(MapInstances.Values, EnumerablePartitionerOptions.NoBuffering);
            Parallel.ForEach(mapInstancePartitioner, new ParallelOptions {MaxDegreeOfParallelism = 8}, mapInstance =>
            {
                var partitioner = Partitioner.Create(
                    portalDao.Where(s => s.SourceMapId.Equals(mapInstance.Map.MapId)),
                    EnumerablePartitionerOptions.None);
                var portalList = new ConcurrentDictionary<int, Portal>();
                Parallel.ForEach(partitioner, portalDto =>
                {
                    Portal portal = portalDto.Adapt<Portal>();
                    portal.SourceMapInstanceId = mapInstance.MapInstanceId;
                    portal.DestinationMapInstanceId = GetBaseMapInstanceIdByMapId(portal.DestinationMapId);
                    portalList[portal.PortalId] = portal;
                });
                mapInstance.Portals.AddRange(portalList.Select(s => s.Value));
            });
            maps.AddRange(mapList.Select(s => s.Value));
        }

        public Guid GetBaseMapInstanceIdByMapId(short mapId)
        {
            return MapInstances.FirstOrDefault(s =>
                s.Value?.Map.MapId == mapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Key;
        }

        public MapInstance GetMapInstance(Guid id)
        {
            return MapInstances.ContainsKey(id) ? MapInstances[id] : null;
        }

        public MapInstance GetBaseMapById(short mapId)
        {
            return MapInstances.FirstOrDefault(s =>
                s.Value?.Map.MapId == mapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Value;
        }
    }
}