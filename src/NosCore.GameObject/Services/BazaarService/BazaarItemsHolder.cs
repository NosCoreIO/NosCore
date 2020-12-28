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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.Services.BazaarService
{
    public class BazaarItemsHolder
    {
        public BazaarItemsHolder(IDao<BazaarItemDto, long> bazaarItemDao,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao, IDao<CharacterDto, long> characterDao)
        {
            var billist = bazaarItemDao.LoadAll().ToList();
            var bzItemInstanceIds = billist.Select(o => o.ItemInstanceId).ToList();
            var bzCharacterIds = billist.Select(o => o.SellerId).ToList();
            var itemInstancelist = itemInstanceDao.Where(s => bzItemInstanceIds.Contains(s!.Id))?.ToList() ?? new List<IItemInstanceDto?>();
            var characterList = characterDao.Where(s => bzCharacterIds.Contains(s.CharacterId))?.ToList() ?? new List<CharacterDto>();

            BazaarItems = new ConcurrentDictionary<long, BazaarLink>(billist.ToDictionary(x => x.BazaarItemId,
                x => new BazaarLink
                {
                    ItemInstance = (ItemInstanceDto?)itemInstancelist.First(s => s!.Id == x.ItemInstanceId),
                    BazaarItem = x, SellerName = characterList.First(s => s.CharacterId == x.SellerId).Name!
                }));
        }

        public ConcurrentDictionary<long, BazaarLink> BazaarItems { get; set; }
    }
}