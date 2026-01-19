//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Services.IdService;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Drops;
using System;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public class MapItemGenerationService(
            EventLoaderService<MapItem, Tuple<MapItem, GetPacket>, IGetMapItemEventHandler> runner,
            IIdService<MapItem> mapItemIdService)
        : IMapItemGenerationService
    {
        private readonly IEventLoaderService<MapItem, Tuple<MapItem, GetPacket>> _runner = runner;

        public MapItem Create(MapInstance mapInstance, IItemInstance itemInstance, short positionX, short positionY)
        {
            var mapItem = new MapItem(mapItemIdService.GetNextId())
            {
                MapInstance = mapInstance,
                ItemInstance = itemInstance,
                PositionX = positionX,
                PositionY = positionY
            };
            _runner.LoadHandlers(mapItem);
            return mapItem;
        }
    }
}
