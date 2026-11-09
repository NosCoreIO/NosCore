//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Core.Services.IdService;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public class MapItemGenerationService(IIdService<MapItemComponentBundle> mapItemIdService, IClock clock)
        : IMapItemGenerationService
    {
        public MapItemComponentBundle Create(MapInstance mapInstance, IItemInstance itemInstance, short positionX, short positionY,
            long? ownerId = null)
        {
            var visualId = mapItemIdService.GetNextId();
            var entity = mapInstance.EcsWorld.CreateMapItem(
                visualId,
                itemInstance.ItemVNum,
                itemInstance.Amount,
                mapInstance.MapInstanceId,
                positionX,
                positionY,
                ownerId,
                clock.GetCurrentInstant(),
                itemInstance.Id,
                itemInstance);
            return new MapItemComponentBundle(entity, mapInstance.EcsWorld);
        }
    }
}
