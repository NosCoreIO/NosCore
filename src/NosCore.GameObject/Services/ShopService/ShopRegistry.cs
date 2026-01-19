//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.ShopService
{
    public class ShopRegistry : IShopRegistry
    {
        private readonly ConcurrentDictionary<long, Shop> _playerShops = new();

        public void RegisterPlayerShop(long characterId, Shop shop)
        {
            _playerShops[characterId] = shop;
        }

        public void UnregisterPlayerShop(long characterId)
        {
            _playerShops.TryRemove(characterId, out _);
        }

        public Shop? GetPlayerShop(long characterId)
        {
            _playerShops.TryGetValue(characterId, out var shop);
            return shop;
        }

        public IEnumerable<Shop> GetAllPlayerShops()
        {
            return _playerShops.Values;
        }
    }
}
