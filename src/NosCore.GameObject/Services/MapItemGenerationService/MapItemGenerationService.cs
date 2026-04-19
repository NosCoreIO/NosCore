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
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public class MapItemGenerationService(
            IEnumerable<IGetMapItemEventHandler> handlers,
            IIdService<MapItemComponentBundle> mapItemIdService)
        : IMapItemGenerationService
    {
        private readonly List<IGetMapItemEventHandler> _handlers = new(handlers);

        public MapItemComponentBundle Create(MapInstance mapInstance, IItemInstance itemInstance, short positionX, short positionY)
        {
            var visualId = mapItemIdService.GetNextId();
            var entity = mapInstance.EcsWorld.CreateMapItem(
                visualId,
                itemInstance.ItemVNum,
                itemInstance.Amount,
                mapInstance.MapInstanceId,
                positionX,
                positionY,
                null,
                Instant.MinValue,
                itemInstance.Id,
                itemInstance);
            var bundle = new MapItemComponentBundle(entity, mapInstance.EcsWorld);
            var context = new MapItemRequestContext();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(bundle))
                {
                    context.PickupSubject.Select(request =>
                    {
                        var task = handler.ExecuteAsync(request);
                        context.HandlerTasks.Add(task);
                        return task;
                    }).Subscribe();
                }
            });
            mapInstance.MapItemRequestContexts[visualId] = context;
            return bundle;
        }
    }
}
