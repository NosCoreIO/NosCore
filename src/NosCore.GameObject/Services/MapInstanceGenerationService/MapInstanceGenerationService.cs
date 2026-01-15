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

using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Holders;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapItemGenerationService;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Shared.I18N;
using NosCore.Networking.SessionGroup;
using NosCore.GameObject.Services.BroadcastService;

namespace NosCore.GameObject.Services.MapInstanceGenerationService
{
    public class MapInstanceGeneratorService(List<MapDto> maps, List<NpcMonsterDto> npcMonsters,
            List<NpcTalkDto> npcTalks, List<ShopDto> shopDtos,
            IMapItemGenerationService mapItemGenerationService, IDao<MapNpcDto, int> mapNpcs,
            IDao<MapMonsterDto, int> mapMonsters, IDao<PortalDto, int> portalDao, IDao<ShopItemDto, int>? shopItems,
            ILogger logger, EventLoaderService<MapInstance,
                MapInstance, IMapInstanceEntranceEventHandler> entranceRunnerService, MapInstanceHolder holder,
            IMapInstanceAccessorService mapInstanceAccessorService,
            IClock clock, ILogLanguageLocalizer<LogLanguageKey> logLanguage, IMapChangeService mapChangeService,
            ISessionGroupFactory sessionGroupFactory, ISessionRegistry sessionRegistry)
        : IMapInstanceGeneratorService
    {
        public Task AddMapInstanceAsync(MapInstance mapInstance)
        {
            holder.MapInstances.TryAdd(mapInstance.MapInstanceId, mapInstance);
            entranceRunnerService.LoadHandlers(mapInstance);
            return LoadPortalsAsync(mapInstance, portalDao.Where(s => s.SourceMapId == mapInstance.Map.MapId)?.ToList() ?? new List<PortalDto>());
        }

        public void RemoveMap(Guid mapInstanceId)
        {
            holder.MapInstances.TryRemove(mapInstanceId, out var mapInstance);
            mapInstance?.Kick();
        }

        public async Task InitializeAsync()
        {
            logger.Information(logLanguage[LogLanguageKey.LOADING_MAPINSTANCES]);
            try
            {
                mapMonsters.LoadAll();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var monsters = mapMonsters.LoadAll().Adapt<IEnumerable<MapMonster>>().GroupBy(u => u.MapId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var npcs = mapNpcs.LoadAll().Adapt<IEnumerable<MapNpc>>().GroupBy(u => u.MapId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var portals = portalDao.LoadAll().GroupBy(s => s.SourceMapId).ToDictionary(x => x.Key, x => x.ToList());

            var mapsdic = maps.ToDictionary(x => x.MapId, x => Guid.NewGuid());
            holder.MapInstances = new ConcurrentDictionary<Guid, MapInstance>(maps.Adapt<List<Map.Map>>().ToDictionary(
                map => mapsdic[map.MapId],
                map =>
                {
                    var mapinstance = CreateMapInstance(map, mapsdic[map.MapId], map.ShopAllowed, MapInstanceType.BaseMapInstance);

                    if (monsters.TryGetValue(map.MapId, out var monster))
                    {
                        monster.ForEach(s => s.Initialize(npcMonsters.Find(o => o.NpcMonsterVNum == s.VNum)!));
                        mapinstance.LoadMonsters(monster);
                    }

                    if (npcs.TryGetValue(map.MapId, out var npc))
                    {
                        npc.ForEach(s =>
                        {
                            List<ShopItemDto> dtoShopItems = new List<ShopItemDto>();
                            NpcTalkDto? dialog = null;
                            var shop = shopDtos.Find(o => o.MapNpcId == s.MapNpcId);
                            if (shop != null)
                            {
                                dtoShopItems = shopItems!.Where(o => o.ShopId == shop.ShopId)!.ToList();
                                dialog = npcTalks!.Find(o => o.DialogId == s.Dialog);
                            }
                            s.Initialize(npcMonsters.Find(o => o.NpcMonsterVNum == s.VNum)!, shop, dialog, dtoShopItems);
                        });
                        mapinstance.LoadNpcs(npc);
                    }
                    entranceRunnerService.LoadHandlers(mapinstance);
                    return mapinstance;
                }));

            await Task.WhenAll(holder.MapInstances.Values.Select(s => s.StartLifeAsync())).ConfigureAwait(false);
            await Task.WhenAll(holder.MapInstances.Values.Select(mapInstance => portals.TryGetValue(mapInstance.Map.MapId, out var portal) ? LoadPortalsAsync(mapInstance, portal) : Task.CompletedTask)).ConfigureAwait(false);
        }

        public MapInstance CreateMapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType normalInstance)
        {
            return new MapInstance(map, guid, shopAllowed, normalInstance, mapItemGenerationService, logger, clock, mapChangeService, sessionGroupFactory, sessionRegistry);
        }

        private async Task LoadPortalsAsync(MapInstance mapInstance, List<PortalDto> portals)
        {
            var portalList = await Task.WhenAll(portals.Adapt<List<Portal>>().Select(portal =>
             {
                 portal.SourceMapInstanceId = mapInstance.MapInstanceId;
                 if (portal.DestinationMapInstanceId == default)
                 {
                     var destination = mapInstanceAccessorService.GetBaseMapById(portal.DestinationMapId);
                     if (destination == null)
                     {
                         return Task.FromResult(portal);
                     }
                     portal.DestinationMapInstanceId = destination.MapInstanceId;
                 }

                 return Task.FromResult(portal);
             })).ConfigureAwait(false);

            mapInstance.Portals.AddRange(portalList);
        }
    }
}