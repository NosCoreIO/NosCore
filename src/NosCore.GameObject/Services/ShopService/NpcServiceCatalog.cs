//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Enumerations.Shops;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.ShopService
{
    public class NpcServiceCatalog(List<ShopTypeServiceDto> shopTypeServices) : INpcServiceCatalog, ISingletonService
    {
        private readonly HashSet<(byte ShopType, NpcService Service)> _offered =
            shopTypeServices.Select(s => (s.ShopType, s.Service)).ToHashSet();

        public bool Offers(IAliveEntity owner, NpcService service) =>
            owner.Shop is { } shop && _offered.Contains((shop.ShopType, service));
    }
}
