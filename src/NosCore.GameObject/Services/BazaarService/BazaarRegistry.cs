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
using System.Linq;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.Services.BazaarService
{
    public class BazaarRegistry : IBazaarRegistry
    {
        private ConcurrentDictionary<long, BazaarLink> _bazaarItems = new();

        public void Initialize(IDictionary<long, BazaarLink> bazaarItems)
        {
            _bazaarItems = new ConcurrentDictionary<long, BazaarLink>(bazaarItems);
        }

        public BazaarLink? GetBazaarItem(long bazaarItemId)
        {
            return _bazaarItems.TryGetValue(bazaarItemId, out var item) ? item : null;
        }

        public IEnumerable<BazaarLink> GetAll()
        {
            return _bazaarItems.Values;
        }

        public IEnumerable<BazaarLink> FindBySellerId(long sellerId)
        {
            return _bazaarItems.Values.Where(s => s.BazaarItem?.SellerId == sellerId);
        }

        public bool TryRegister(long bazaarItemId, BazaarLink bazaarLink)
        {
            return _bazaarItems.TryAdd(bazaarItemId, bazaarLink);
        }

        public bool TryUnregister(long bazaarItemId, out BazaarLink? bazaarLink)
        {
            return _bazaarItems.TryRemove(bazaarItemId, out bazaarLink);
        }

        public void Update(long bazaarItemId, BazaarLink bazaarLink)
        {
            _bazaarItems[bazaarItemId] = bazaarLink;
        }
    }
}
