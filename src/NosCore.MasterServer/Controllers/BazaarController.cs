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
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.Enumerations;
using Mapster;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Bazaar;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.WebApi;
using NosCore.MasterServer.DataHolders;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class BazaarController : Controller
    {
        private readonly IGenericDao<BazaarItemDto> _bazaarItemDao;
        private readonly BazaarItemsHolder _holder;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;

        public BazaarController(BazaarItemsHolder holder, IGenericDao<BazaarItemDto> bazaarItemDao,
            IGenericDao<IItemInstanceDto> itemInstanceDao)
        {
            _bazaarItemDao = bazaarItemDao;
            _itemInstanceDao = itemInstanceDao;
            _holder = holder;
        }
#pragma warning disable IDE0060 // Supprimer le paramètre inutilisé
        [HttpGet]
        public List<BazaarLink> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter)
#pragma warning restore IDE0060 // Supprimer le paramètre inutilisé
        {
            var bzlist = new List<BazaarLink>();
#pragma warning disable CS0219 // Variable is assigned but its value is never used
            var applyRareFilter = false;
            var applyUpgradeFilter = false;
            var applySpLevelFilter = false;
            var applyLevelFilter = false;
            var applyClassFilter = false;
            PocketType? pocketType = null;
            ItemType? itemType = null;
            byte? subtypeFilter = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
            IEnumerable<BazaarLink> bzlinks;
            if (id != -1)
            {
                bzlinks = _holder.BazaarItems.Values.Where(s => s.BazaarItem.BazaarItemId == id);
            }
            else
            {
                bzlinks = _holder.BazaarItems.Values.Where(s =>
                    (s.BazaarItem.SellerId == sellerFilter) || (sellerFilter == null));
            }

            foreach (var bz in bzlinks)
            {
                switch (typeFilter)
                {
                    case BazaarListType.Weapon:
                        itemType = ItemType.Weapon;
                        pocketType = PocketType.Equipment;
                        applyClassFilter = true;
                        applyLevelFilter = true;
                        applyRareFilter = true;
                        applyUpgradeFilter = true;
                        break;

                    case BazaarListType.Armor:
                        itemType = ItemType.Armor;
                        pocketType = PocketType.Equipment;
                        applyClassFilter = true;
                        applyLevelFilter = true;
                        applyRareFilter = true;
                        applyUpgradeFilter = true;
                        break;

                    case BazaarListType.Equipment:
                        itemType = ItemType.Fashion;
                        pocketType = PocketType.Equipment;
                        if (subTypeFilter > 0)
                        {
                            var equipmentTypeFilter = (BazaarEquipmentType) subTypeFilter;
                        }

                        applyLevelFilter = true;
                        break;

                    case BazaarListType.Jewelery:
                        itemType = ItemType.Jewelery;
                        pocketType = PocketType.Equipment;
                        if (subTypeFilter > 0)
                        {
                            var jeweleryTypeFilter = (BazaarJeweleryType) subTypeFilter;
                        }

                        applyLevelFilter = true;
                        break;

                    case BazaarListType.Specialist:
                    case BazaarListType.Npc:
                    case BazaarListType.Pet:
                        pocketType = PocketType.Equipment;
                        itemType = ItemType.Box;
                        applySpLevelFilter = true;
                        break;

                    case BazaarListType.Shell:
                        pocketType = PocketType.Equipment;
                        itemType = ItemType.Shell;
                        applySpLevelFilter = true;
                        if (subTypeFilter > 0)
                        {
                            subtypeFilter = subTypeFilter++;
                            applyRareFilter = true;
                        }

                        break;

                    case BazaarListType.Main:
                        pocketType = PocketType.Main;
                        if (subTypeFilter > 0)
                        {
                            var mainTypeFilter = (BazaarMainType) subTypeFilter;
                        }

                        break;

                    case BazaarListType.Usable:
                        pocketType = PocketType.Etc;
                        if (subTypeFilter > 0)
                        {
                            var bazaarTypeFilter = (BazaarUsableType) subTypeFilter;
                        }

                        break;

                    case BazaarListType.Other:
                        pocketType = PocketType.Equipment;
                        itemType = ItemType.Box;
                        break;

                    case BazaarListType.Vehicle:
                        itemType = ItemType.Box;
                        break;
                }

                bzlist.Add(bz);
            }

            //todo this need to be move to the filter when done
            return bzlist.Skip(index ?? 0 * pageSize ?? 0).Take((byte) (pageSize ?? bzlist.Count)).ToList();
        }


        [HttpDelete]
        public bool DeleteBazaar(long id, short count, string requestCharacterName)
        {
            var bzlink = _holder.BazaarItems.Values.FirstOrDefault(s => s.BazaarItem.BazaarItemId == id);
            if (bzlink == null)
            {
                throw new ArgumentException();
            }

            if ((bzlink.ItemInstance.Amount - count < 0) || (count < 0))
            {
                return false;
            }

            if ((bzlink.ItemInstance.Amount == count) && (requestCharacterName == bzlink.SellerName))
            {
                _bazaarItemDao.Delete(bzlink.BazaarItem.BazaarItemId);
                _holder.BazaarItems.TryRemove(bzlink.BazaarItem.BazaarItemId, out _);
                _itemInstanceDao.Delete(bzlink.ItemInstance.Id);
            }
            else
            {
                var item = (IItemInstanceDto) bzlink.ItemInstance;
                item.Amount -= count;
                _itemInstanceDao.InsertOrUpdate(ref item);
            }

            return true;
        }

        [HttpPost]
        public LanguageKey AddBazaar([FromBody] BazaarRequest bazaarRequest)
        {
            var items = _holder.BazaarItems.Values.Where(o => o.BazaarItem.SellerId == bazaarRequest.CharacterId);
            if (items.Count() > 10 * (bazaarRequest.HasMedal ? 10 : 1) - 1)
            {
                return LanguageKey.LIMIT_EXCEEDED;
            }

            var item = _itemInstanceDao.FirstOrDefault(s => s.Id == bazaarRequest.ItemInstanceId);
            if ((item == null) || (item.Amount < bazaarRequest.Amount) || (bazaarRequest.Amount < 0) ||
                (bazaarRequest.Price < 0))
            {
                throw new ArgumentException();
            }

            Guid itemId;
            if (item.Amount == bazaarRequest.Amount)
            {
                itemId = item.Id;
            }
            else
            {
                itemId = item.Id;
                item.Amount -= bazaarRequest.Amount;
                _itemInstanceDao.InsertOrUpdate(ref item);
                item.Id = Guid.NewGuid();
            }

            _itemInstanceDao.InsertOrUpdate(ref item);

            var bazaarItem = new BazaarItemDto
            {
                Amount = bazaarRequest.Amount,
                DateStart = SystemTime.Now(),
                Duration = bazaarRequest.Duration,
                IsPackage = bazaarRequest.IsPackage,
                MedalUsed = bazaarRequest.HasMedal,
                Price = bazaarRequest.Price,
                SellerId = bazaarRequest.CharacterId,
                ItemInstanceId = item.Id
            };
            _bazaarItemDao.InsertOrUpdate(ref bazaarItem);
            _holder.BazaarItems.TryAdd(bazaarItem.BazaarItemId,
                new BazaarLink
                {
                    BazaarItem = bazaarItem, SellerName = bazaarRequest.CharacterName,
                    ItemInstance = item.Adapt<ItemInstanceDto>()
                });

            return LanguageKey.OBJECT_IN_BAZAAR;
        }

        [HttpPatch]
        public BazaarLink? ModifyBazaar(long id, [FromBody] JsonPatchDocument<BazaarLink> bzMod)
        {
            var item = _holder.BazaarItems.Values
                .FirstOrDefault(o => o.BazaarItem.BazaarItemId == id);
            if ((item == null) || (item.BazaarItem.Amount != item.ItemInstance.Amount))
            {
                return null;
            }

            bzMod.ApplyTo(item);
            var bz = item.BazaarItem;
            _bazaarItemDao.InsertOrUpdate(ref bz);
            return item;

        }
    }
}