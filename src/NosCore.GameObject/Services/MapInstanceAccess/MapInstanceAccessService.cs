//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using NosCore.Data.StaticEntities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using NosCore.DAL;
using NosCore.GameObject.Services.PortalGeneration;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.MapInstanceAccess
{
    public class MapInstanceAccessService
    {
        public readonly ConcurrentDictionary<Guid, MapInstance> MapInstances =
            new ConcurrentDictionary<Guid, MapInstance>();

        public MapInstanceAccessService(List<NpcMonsterDTO> npcMonsters, List<Map.Map> maps)
        {
            var mapPartitioner = Partitioner.Create(maps, EnumerablePartitionerOptions.NoBuffering);
            var mapList = new ConcurrentDictionary<short, Map.Map>();
            var npccount = 0;
            var monstercount = 0;
            Parallel.ForEach(mapPartitioner, new ParallelOptions { MaxDegreeOfParallelism = 8 }, map =>
            {
                var guid = Guid.NewGuid();
                map.Initialize();
                mapList[map.MapId] = map;
                var newMap = new MapInstance(map, guid, map.ShopAllowed, MapInstanceType.BaseMapInstance, npcMonsters);
                MapInstances.TryAdd(guid, newMap);
                newMap.LoadMonsters();
                newMap.LoadNpcs();
                newMap.StartLife();
                monstercount += newMap.Monsters.Count;
                npccount += newMap.Npcs.Count;
            });
            var mapInstancePartitioner = Partitioner.Create(MapInstances.Values, EnumerablePartitionerOptions.NoBuffering);
            Parallel.ForEach(mapInstancePartitioner, new ParallelOptions {MaxDegreeOfParallelism = 8}, mapInstance =>
            {
                var partitioner = Partitioner.Create(
                    DAOFactory.PortalDAO.Where(s => s.SourceMapId.Equals(mapInstance.Map.MapId)),
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
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPNPCS_LOADED),
                npccount));
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPMONSTERS_LOADED),
                monstercount));
        }

        public  Guid GetBaseMapInstanceIdByMapId(short mapId)
        {
            return MapInstances.FirstOrDefault(s =>
                s.Value?.Map.MapId == mapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Key;
        }

        public  MapInstance GetMapInstance(Guid id)
        {
            return MapInstances.ContainsKey(id) ? MapInstances[id] : null;
        }
    }
}
