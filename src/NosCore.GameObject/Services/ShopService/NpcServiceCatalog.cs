//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.ShopService
{
    public interface INpcServiceCatalog
    {
        bool Offers(IAliveEntity entity, NpcService service);
    }

    public class NpcServiceCatalog(List<ShopTypeServiceDto> shopTypeServices) : INpcServiceCatalog, ISingletonService
    {
        public bool Offers(IAliveEntity entity, NpcService service) =>
            entity.Shop is { } shop
            && shopTypeServices.Exists(s => s.ShopType == shop.ShopType && s.Service == service);
    }
}
