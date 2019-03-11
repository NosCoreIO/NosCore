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
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mapster;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapItemProvider;
using Serilog;

namespace NosCore.GameObject.Providers.MapInstanceProvider
{
    public class MapInstanceProvider : IMapInstanceProvider
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IMapItemProvider _mapItemProvider;
        private readonly IGenericDao<MapMonsterDto> _mapMonsters;
        private readonly IGenericDao<PortalDto> _portalDao;
        private readonly IAdapter _adapter;
        private readonly List<MapDto> _maps;
        private readonly List<NpcMonsterDto> _npcMonsters;
        private readonly IGenericDao<MapNpcDto> _mapNpcs;
        private ConcurrentDictionary<Guid, MapInstance> MapInstances =
            new ConcurrentDictionary<Guid, MapInstance>();

        public MapInstanceProvider(List<NpcMonsterDto> npcMonsters, List<MapDto> maps,
            IMapItemProvider mapItemProvider, IGenericDao<MapNpcDto> mapNpcs,
            IGenericDao<MapMonsterDto> mapMonsters, IGenericDao<PortalDto> portalDao, IAdapter adapter)
        {
            _mapItemProvider = mapItemProvider;
            _mapMonsters = mapMonsters;
            _portalDao = portalDao;
            _adapter = adapter;
            _maps = maps;
            _mapNpcs = mapNpcs;
            _npcMonsters = npcMonsters;
        }



        public void Initialize()
        {
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LOADING_MAPINSTANCES));
            var monsters = _mapMonsters.LoadAll().GroupBy(u => u.MapId).ToList();
            var npcs = _mapNpcs.LoadAll().GroupBy(u => u.MapId).ToList();
            var portals = _portalDao.LoadAll().ToList();
            var mapsdic = _maps.ToDictionary(x => x.MapId, x => Guid.NewGuid());

            MapInstances = new ConcurrentDictionary<Guid, MapInstance>(_maps.Adapt<List<Map.Map>>().ToDictionary(
                map => mapsdic[map.MapId],
                map =>
                {
                    var mapinstance = new MapInstance(map, mapsdic[map.MapId], map.ShopAllowed, MapInstanceType.BaseMapInstance,
                        _mapItemProvider, _adapter);
                    var monst = monsters.SingleOrDefault(s => s.Key == map.MapId)?.ToList();
                    if (monst != null)
                    {
                        mapinstance.LoadMonsters(monst);
                    }
                    var npclist = npcs.SingleOrDefault(s => s.Key == map.MapId)?.ToList();
                    if (npclist != null)
                    {
                        mapinstance.LoadNpcs(npclist);
                    }
                    mapinstance.StartLife();
                    return mapinstance;
                }));

            var mapInstancePartitioner = Partitioner.Create(MapInstances.Values, EnumerablePartitionerOptions.NoBuffering);
            Parallel.ForEach(mapInstancePartitioner, mapInstance =>
            {
                var partitioner = Partitioner.Create(
                    portals.Where(s => s.SourceMapId == mapInstance.Map.MapId).Adapt<List<Portal>>(),
                    EnumerablePartitionerOptions.None);
                var portalList = new ConcurrentDictionary<int, Portal>();
                Parallel.ForEach(partitioner, portal =>
                {
                    portal.SourceMapInstanceId = mapInstance.MapInstanceId;
                    portal.DestinationMapInstanceId = GetBaseMapInstanceIdByMapId(portal.DestinationMapId);
                    portalList[portal.PortalId] = portal;
                });
                mapInstance.Portals.AddRange(portalList.Select(s => s.Value));
            });
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