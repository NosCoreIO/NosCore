//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.GameObject.Services.ShopService
{
    public interface IShopRegistry
    {
        void RegisterPlayerShop(long characterId, Shop shop);
        void UnregisterPlayerShop(long characterId);
        Shop? GetPlayerShop(long characterId);
        IEnumerable<Shop> GetAllPlayerShops();
    }
}
