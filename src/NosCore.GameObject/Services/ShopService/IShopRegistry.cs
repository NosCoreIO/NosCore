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

using Arch.Core;
using System.Collections.Concurrent;

namespace NosCore.GameObject.Services.ShopService
{
    public interface IShopRegistry
    {
        void Register(Entity entity, Shop shop);
        Shop? GetShop(Entity entity);
        void Remove(Entity entity);
    }

    public class ShopRegistry : IShopRegistry
    {
        private readonly ConcurrentDictionary<Entity, Shop> _shops = new();

        public void Register(Entity entity, Shop shop)
        {
            _shops[entity] = shop;
        }

        public Shop? GetShop(Entity entity)
        {
            return _shops.TryGetValue(entity, out var shop) ? shop : null;
        }

        public void Remove(Entity entity)
        {
            _shops.TryRemove(entity, out _);
        }
    }
}
