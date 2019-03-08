//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Threading.Tasks;
using Mapster;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.MapInstanceProvider;

namespace NosCore.GameObject.Providers.MapNpcProvider
{
    public class MapNpcProvider : IMapNpcProvider
    {
        private readonly IItemProvider _itemProvider;
        private readonly List<MapNpcDto> _mapNpcs;
        private readonly List<NpcMonsterDto> _npcMonsters;
        private readonly List<ShopItemDto> _shopItems;
        private readonly List<ShopDto> _shops;

        public MapNpcProvider(IItemProvider itemProvider, List<ShopDto> shops,
            List<ShopItemDto> shopItems,
            List<NpcMonsterDto> npcMonsters, List<MapNpcDto> mapNpcs)
        {
            _itemProvider = itemProvider;
            _npcMonsters = npcMonsters;
            _shops = shops;
            _shopItems = shopItems;
            _mapNpcs = mapNpcs;
        }

        public ConcurrentDictionary<long, MapNpc> Create(MapInstance mapInstance)
        {
            var npcs = new ConcurrentDictionary<long, MapNpc>();
            var partitioner = Partitioner.Create(_mapNpcs.Where(s => s.MapId == mapInstance.Map.MapId),
                EnumerablePartitionerOptions.None);
            Parallel.ForEach(partitioner, npc =>
            {
                MapNpc mapNpc = npc.Adapt<MapNpc>();
                mapNpc.Initialize(_npcMonsters.Find(s => s.NpcMonsterVNum == mapNpc.VNum));
                mapNpc.MapInstance = mapInstance;
                mapNpc.MapInstanceId = mapInstance.MapInstanceId;
                mapNpc.Shop = LoadShop(mapNpc.MapNpcId);
                npcs[mapNpc.MapNpcId] = mapNpc;
            });

            return npcs;
        }

        private Shop LoadShop(int mapNpcId)
        {
            Shop shop = null;
            var shopObj = DaoFactory.GetGenericDao<ShopDto>().FirstOrDefault(s => s.MapNpcId == mapNpcId);
            if (shopObj != null)
            {
                var shopItemsDto = DaoFactory.GetGenericDao<ShopItemDto>().Where(s => s.ShopId == shopObj.ShopId);
                var shopItems = new ConcurrentDictionary<int, ShopItem>();
                Parallel.ForEach(shopItemsDto, shopItemGrouping =>
                {
                    var shopItem = shopItemGrouping.Adapt<ShopItem>();
                    shopItem.ItemInstance = _itemProvider.Create(shopItemGrouping.ItemVNum, -1);
                    shopItems[shopItemGrouping.ShopItemId] = shopItem;
                });
                shop = shopObj.Adapt<Shop>();
                shop.ShopItems = shopItems;
            }

            return shop;
        }
    }
}