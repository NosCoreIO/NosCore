using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChickenAPI.Packets.Enumerations;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Bazaar;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.WebApi;
using NosCore.Database.Entities;
using NosCore.MasterServer.DataHolders;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class BazaarController : Controller
    {
        private readonly BazaarItemsHolder _holder;
        private readonly IGenericDao<BazaarItemDto> _bazaarItemDao;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;

        public BazaarController(BazaarItemsHolder holder, IGenericDao<BazaarItemDto> bazaarItemDao, IGenericDao<IItemInstanceDto> itemInstanceDao)
        {
            _bazaarItemDao = bazaarItemDao;
            _itemInstanceDao = itemInstanceDao;
            _holder = holder;
        }

        [HttpGet]
        public List<BazaarLink> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? TypeFilter, byte? SubTypeFilter, byte? LevelFilter, byte? RareFilter, byte? UpgradeFilter, long? sellerFilter)
        {
            var bzlist = new List<Data.WebApi.BazaarLink>();

            var applyRareFilter = false;
            var applyUpgradeFilter = false;
            var applySpLevelFilter = false;
            var applyLevelFilter = false;
            var applyClassFilter = false;
            PocketType? pocketType = null;
            ItemType? itemType = null;
            byte? subtypeFilter = null;
            IEnumerable<BazaarLink> bzlinks;
            if (id != -1)
            {
                bzlinks = _holder.BazaarItems.Values.Where(s => s.BazaarItem.BazaarItemId == id);
            }
            else
            {
                bzlinks = _holder.BazaarItems.Values.Where(s =>  s.BazaarItem.SellerId == sellerFilter || sellerFilter == null);
            }

            foreach (var bz in bzlinks)
            {
                switch (TypeFilter)
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
                        if (SubTypeFilter > 0)
                        {
                            var equipmentTypeFilter = (BazaarEquipmentType)SubTypeFilter;
                        }
                        applyLevelFilter = true;
                        break;

                    case BazaarListType.Jewelery:
                        itemType = ItemType.Jewelery;
                        pocketType = PocketType.Equipment;
                        if (SubTypeFilter > 0)
                        {
                            var jeweleryTypeFilter = (BazaarJeweleryType)SubTypeFilter;
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
                        if (SubTypeFilter > 0)
                        {
                            subtypeFilter = SubTypeFilter++;
                            applyRareFilter = true;
                        }

                        break;

                    case BazaarListType.Main:
                        pocketType = PocketType.Main;
                        if (SubTypeFilter > 0)
                        {
                            var mainTypeFilter = (BazaarMainType)SubTypeFilter;
                        }
                        break;

                    case BazaarListType.Usable:
                        pocketType = PocketType.Etc;
                        if (SubTypeFilter > 0)
                        {
                            var bazaarTypeFilter = (BazaarUsableType)SubTypeFilter;
                        }

                        break;

                    case BazaarListType.Other:
                        pocketType = PocketType.Equipment;
                        itemType = ItemType.Box;
                        break;

                    case BazaarListType.Vehicle:
                        itemType = ItemType.Box;
                        break;

                    default:
                        break;
                }
                bzlist.Add(bz);
            }
            //todo this need to be move to the filter when done
            return bzlist.Skip((int)(index * pageSize)).Take((byte)pageSize).ToList();
        }

        [HttpPost]
        public LanguageKey AddBazaar([FromBody] BazaarRequest bazaarRequest)
        {
            var items = _holder.BazaarItems.Values.Where(o => o.BazaarItem.SellerId == bazaarRequest.CharacterId);
            if (items.Count() > 10 * (bazaarRequest.HasMedal ? 1 : 10))
            {
                return LanguageKey.LIMIT_EXCEEDED;
            }

            var item = _itemInstanceDao.FirstOrDefault(s => s.Id == bazaarRequest.ItemInstanceId);
            Guid itemId;
            if (item.Amount == bazaarRequest.Amount)
            {
                itemId = item.Id;
            }
            else
            {
                itemId = item.Id;
                item.Amount = bazaarRequest.Amount;
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
                ItemInstanceId = itemId
            };
            _holder.BazaarItems.TryAdd(bazaarItem.SellerId,
                new BazaarLink
                { BazaarItem = bazaarItem, SellerName = bazaarRequest.CharacterName, ItemInstance = item.Adapt<ItemInstanceDto>() });
            _bazaarItemDao.InsertOrUpdate(ref bazaarItem);
            return LanguageKey.OBJECT_IN_BAZAAR;
        }
    }
}
