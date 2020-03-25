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
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.GameObject
{
    public class Shop : ShopDto
    {
        private int? _size;

        public Shop()
        {
            ShopItems = new ConcurrentDictionary<int, ShopItem>();
        }

        public ConcurrentDictionary<int, ShopItem> ShopItems { get; set; }

        public ClientSession? Session { get; set; }
        public long Sell { get; internal set; }

        public int Size
        {
            get => _size ?? ShopItems.Values.Max(s => s.Slot) + 1;
            set => _size = value;
        }
    }
}