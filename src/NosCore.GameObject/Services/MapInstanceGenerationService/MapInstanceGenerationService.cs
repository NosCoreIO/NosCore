//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Networking.SessionGroup;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapInstanceGenerationService
{
    public class MapInstanceGeneratorService(List<MapDto> maps, List<NpcMonsterDto> npcMonsters,
            List<NpcTalkDto> npcTalks, List<ShopDto> shopDtos,
            IMapItemGenerationService mapItemGenerationService, IDao<MapNpcDto, int> mapNpcs,
            IDao<MapMonsterDto, int> mapMonsters, IDao<PortalDto, int> portalDao, IDao<ShopItemDto, int>? shopItems,
            ILogger logger, EventLoaderService<MapInstance,
                MapInstance, IMapInstanceEntranceEventHandler> entranceRunnerService, IMapInstanceRegistry mapInstanceRegistry,
            IMapInstanceAccessorService mapInstanceAccessorService,
            IClock clock, ILogLanguageLocalizer<LogLanguageKey> logLanguage, IMapChangeService mapChangeService,
            ISessionGroupFactory sessionGroupFactory, ISessionRegistry sessionRegistry, IItemGenerationService itemProvider, IHeuristic distanceCalculator)
        : IMapInstanceGeneratorService
    {
        public Task AddMapInstanceAsync(MapInstance mapInstance)
        {
            mapInstanceRegistry.Register(mapInstance.MapInstanceId, mapInstance);
            entranceRunnerService.LoadHandlers(mapInstance);
            return LoadPortalsAsync(mapInstance, portalDao.Where(s => s.SourceMapId == mapInstance.Map.MapId)?.ToList() ?? new List<PortalDto>());
        }

        public void RemoveMap(Guid mapInstanceId)
        {
            if (mapInstanceRegistry.Unregister(mapInstanceId, out var mapInstance))
            {
                mapInstance?.Kick();
            }
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
            var mapInstances = maps.Adapt<List<Map.Map>>().ToDictionary(
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
                                dialog = npcTalks.Find(o => o.DialogId == s.Dialog);
                            }
                            s.Initialize(npcMonsters.Find(o => o.NpcMonsterVNum == s.VNum)!, shop, dialog, dtoShopItems, itemProvider);
                        });
                        mapinstance.LoadNpcs(npc);
                    }
                    entranceRunnerService.LoadHandlers(mapinstance);
                    return mapinstance;
                });

            mapInstanceRegistry.SetAll(mapInstances);

            await Task.WhenAll(mapInstanceRegistry.GetAll().Select(s => s.StartLifeAsync()));
            await Task.WhenAll(mapInstanceRegistry.GetAll().Select(mapInstance => portals.TryGetValue(mapInstance.Map.MapId, out var portal) ? LoadPortalsAsync(mapInstance, portal) : Task.CompletedTask));
        }

        public MapInstance CreateMapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType normalInstance)
        {
            return new MapInstance(map, guid, shopAllowed, normalInstance, mapItemGenerationService, logger, clock, mapChangeService, sessionGroupFactory, sessionRegistry, distanceCalculator);
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
             }));

            mapInstance.Portals.AddRange(portalList);
        }
    }
}
