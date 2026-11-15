//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Shops;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.ShopService
{
    public interface INpcServiceCatalog
    {
        // Whether the entity's shop offers the service, as its shop type lists it in shoptype.dat.
        bool Offers(IAliveEntity owner, NpcService service);
    }
}
