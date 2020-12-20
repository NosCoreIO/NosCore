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
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Providers.MapInstanceProvider.Handlers;
using NosCore.GameObject.Providers.MapItemProvider;
using Serilog;

namespace NosCore.GameObject.Providers.MapInstanceProvider
{
    public class MapInstanceProvider : IMapInstanceProvider
    {
        private readonly ILogger _logger;
        private readonly List<ShopDto> _shopDtos;
        private readonly IDao<ShopItemDto, int>? _shopItems;
        private readonly IMapItemProvider _mapItemProvider;
        private readonly IDao<MapMonsterDto, int> _mapMonsters;
        private readonly List<NpcTalkDto> _npcTalks;
        private readonly IDao<MapNpcDto, int> _mapNpcs;
        private readonly List<MapDto> _maps;
        private readonly IDao<PortalDto, int> _portalDao;
        private readonly List<NpcMonsterDto> _npcMonsters;
        private ConcurrentDictionary<Guid, MapInstance> _mapInstances =
            new ConcurrentDictionary<Guid, MapInstance>();

        public MapInstanceProvider(List<MapDto> maps, List<NpcMonsterDto> npcMonsters, List<NpcTalkDto> npcTalks, List<ShopDto> shopDtos,
            IMapItemProvider mapItemProvider, IDao<MapNpcDto, int> mapNpcs,
            IDao<MapMonsterDto, int> mapMonsters, IDao<PortalDto, int> portalDao, IDao<ShopItemDto, int>? shopItems, ILogger logger)
        {
            _mapItemProvider = mapItemProvider;
            _npcTalks = npcTalks;
            _npcMonsters = npcMonsters;
            _mapMonsters = mapMonsters;
            _portalDao = portalDao;
            _maps = maps;
            _mapNpcs = mapNpcs;
            _logger = logger;
            _shopItems = shopItems;
            _shopDtos = shopDtos;
        }

        public Task AddMapInstanceAsync(MapInstance mapInstance)
        {
            _mapInstances.TryAdd(mapInstance.MapInstanceId, mapInstance);
            return LoadPortalsAsync(mapInstance, _portalDao.Where(s => s.SourceMapId == mapInstance.Map.MapId)?.ToList() ?? new List<PortalDto>());
        }

        public void RemoveMap(Guid mapInstanceId)
        {
            _mapInstances.TryRemove(mapInstanceId, out var mapInstance);
            mapInstance?.Kick();
        }

        public async Task InitializeAsync()
        {
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LOADING_MAPINSTANCES));
            try
            {
                _ = _mapMonsters.LoadAll();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var monsters = _mapMonsters.LoadAll().Adapt<IEnumerable<MapMonster>>().GroupBy(u => u.MapId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var npcs = _mapNpcs.LoadAll().Adapt<IEnumerable<MapNpc>>().GroupBy(u => u.MapId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var portals = _portalDao.LoadAll().GroupBy(s => s.SourceMapId).ToDictionary(x => x.Key, x => x.ToList());

            var mapsdic = _maps.ToDictionary(x => x.MapId, x => Guid.NewGuid());
            _mapInstances = new ConcurrentDictionary<Guid, MapInstance>(_maps.Adapt<List<Map.Map>>().ToDictionary(
                map => mapsdic[map.MapId],
                map =>
                {
                    var mapinstance = CreateMapInstance(map, mapsdic[map.MapId], map.ShopAllowed,
                        MapInstanceType.BaseMapInstance, new List<IMapInstanceEventHandler>());

                    if (monsters.ContainsKey(map.MapId))
                    {
                        var mapMonsters = monsters[map.MapId];
                        mapMonsters.ForEach(s => s.Initialize(_npcMonsters.Find(o => o.NpcMonsterVNum == s.VNum)!));
                        mapinstance.LoadMonsters(mapMonsters);
                    }

                    if (npcs.ContainsKey(map.MapId))
                    {
                        var mapNpcs = npcs[map.MapId];
                        mapNpcs.ForEach(s =>
                        {
                            List<ShopItemDto> shopItems = new List<ShopItemDto>();
                            NpcTalkDto? dialog = null;
                            var shop = _shopDtos.Find(o => o.MapNpcId == s.MapNpcId);
                            if (shop != null)
                            {
                                shopItems = _shopItems!.Where(o => o.ShopId == shop.ShopId)!.ToList();
                                dialog = _npcTalks!.Find(o => o.DialogId == s.Dialog);
                            }
                            s.Initialize(_npcMonsters.Find(o => o.NpcMonsterVNum == s.VNum)!, shop, dialog, shopItems);
                        });
                        mapinstance.LoadNpcs(mapNpcs);
                    }

                    return mapinstance;
                }));

            await Task.WhenAll(_mapInstances.Values.Select(s => s.StartLifeAsync())).ConfigureAwait(false);
            await Task.WhenAll(_mapInstances.Values.Select(mapInstance => portals.ContainsKey(mapInstance.Map.MapId) ? LoadPortalsAsync(mapInstance, portals[mapInstance.Map.MapId]) : Task.CompletedTask)).ConfigureAwait(false);
        }

        public Guid GetBaseMapInstanceIdByMapId(short mapId)
        {
            return _mapInstances.FirstOrDefault(s =>
                (s.Value?.Map.MapId == mapId) && (s.Value.MapInstanceType == MapInstanceType.BaseMapInstance)).Key;
        }

        public MapInstance? GetMapInstance(Guid id)
        {
            return _mapInstances.ContainsKey(id) ? _mapInstances[id] : null;
        }

        public MapInstance GetBaseMapById(short mapId)
        {
            return _mapInstances.FirstOrDefault(s =>
                (s.Value?.Map.MapId == mapId) && (s.Value.MapInstanceType == MapInstanceType.BaseMapInstance)).Value;
        }

        public MapInstance CreateMapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType normalInstance,
            List<IMapInstanceEventHandler> mapInstanceEventHandler)
        {
            return new MapInstance(map, guid, shopAllowed, normalInstance, _mapItemProvider, _logger,
                mapInstanceEventHandler);
        }

        private async Task LoadPortalsAsync(MapInstance mapInstance, List<PortalDto> portals)
        {
            var portalList = await Task.WhenAll(portals.Adapt<List<Portal>>().Select(portal =>
             {
                 portal.SourceMapInstanceId = mapInstance.MapInstanceId;
                 if (portal.DestinationMapInstanceId == default)
                 {
                     portal.DestinationMapInstanceId = GetBaseMapInstanceIdByMapId(portal.DestinationMapId);
                 }

                 return Task.FromResult(portal);
             })).ConfigureAwait(false);

            mapInstance.Portals.AddRange(portalList);
        }
    }
}