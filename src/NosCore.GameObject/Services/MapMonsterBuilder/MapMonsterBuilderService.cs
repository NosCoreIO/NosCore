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
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.GameObject.Services.MapItemBuilder;

namespace NosCore.GameObject.Services.MapNpcBuilder
{
    public class MapMonsterBuilderService : IMapMonsterBuilderService
    {
        private readonly List<Item> _items;
        private readonly List<ShopDto> _shops;
        private readonly List<ShopItemDto> _shopItems;
        private readonly List<MapMonsterDto> _mapMonsters;
        private readonly List<NpcMonsterDto> _npcMonsters;
        public MapMonsterBuilderService(List<Item> items, List<ShopDto> shops, List<ShopItemDto> shopItems,
            List<NpcMonsterDto> npcMonsters, List<MapMonsterDto> mapMonsters)
        {
            _items = items;
            _npcMonsters = npcMonsters;
            _shops = shops;
            _shopItems = shopItems;
            _mapMonsters = mapMonsters;
        }

        public ConcurrentDictionary<long, MapMonster> Create(MapInstance mapInstance)
        {
            var monsters = new ConcurrentDictionary<long, MapMonster>();
            var partitioner = Partitioner.Create(_mapMonsters.Where(s => s.MapId == mapInstance.Map.MapId),
                EnumerablePartitionerOptions.None);
            Parallel.ForEach(partitioner, monster =>
            {
                MapMonster mapMonster = monster.Adapt<MapMonster>();
                mapMonster.Initialize(_npcMonsters.Find(s => s.NpcMonsterVNum == monster.VNum));
                mapMonster.MapInstance = mapInstance;
                mapMonster.MapInstanceId = mapInstance.MapInstanceId;
                monsters[mapMonster.MapMonsterId] = mapMonster;
            });
            
            return monsters;
        }
    }
}