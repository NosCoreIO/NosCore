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
