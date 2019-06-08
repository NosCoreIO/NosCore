using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChickenAPI.Packets.Enumerations;
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

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class BazaarController : Controller
    {
        private readonly IGenericDao<BazaarItemDto> _bazaarItemDao;
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;

        public BazaarController(IGenericDao<BazaarItemDto> bazaarItemDao, IGenericDao<IItemInstanceDto> itemInstanceDao)
        {
            _bazaarItemDao = bazaarItemDao;
            _itemInstanceDao = itemInstanceDao;
        }

        [HttpGet]
        public List<BazaarItemDto> GetBazaar(long id, BazaarListType TypeFilter, byte SubTypeFilter, byte LevelFilter, byte RareFilter, byte UpgradeFilter)
        {
            var bzlist = new List<BazaarItemDto>();
            var billist = _bazaarItemDao.LoadAll();
            var applyRareFilter = false;
            var applyUpgradeFilter = false;
            var applyLevelFilter = false;
            var applyClassFilter = false;
            BazaarEquipmentType? EquipmentTypeFilter = null;
            PocketType PocketType;

            foreach (var bz in billist)
            {
                switch (TypeFilter)
                {
                    case BazaarListType.Weapon:
                        if (bz.ItemInstance.Item.ItemType == ItemType.Weapon)
                        {
                            PocketType = PocketType.Equipment;
                            applyClassFilter = true;
                            applyLevelFilter = true;
                            applyRareFilter = true;
                            applyUpgradeFilter = true;
                        }
                        break;

                    case BazaarListType.Armor:
                        if (bz.ItemInstance.Item.ItemType == ItemType.Armor)
                        {
                            PocketType = PocketType.Equipment;
                            applyClassFilter = true;
                            applyLevelFilter = true;
                            applyRareFilter = true;
                            applyUpgradeFilter = true;
                        }
                        break;

                    case BazaarListType.Equipment:
                        if (bz.ItemInstance.Item.ItemType == ItemType.Fashion)
                        {
                            PocketType = PocketType.Equipment;
                            if (SubTypeFilter > 0)
                            {
                                EquipmentTypeFilter = (BazaarEquipmentType)SubTypeFilter;
                            }
                            applyLevelFilter = true;
                        }
                        break;

                    case BazaarListType.Jewelery:
                        if (bz.ItemInstance.Item.ItemType == ItemType.Jewelery)
                        {
                            PocketType = PocketType.Equipment;
                            if (SubTypeFilter == 0
                                || SubTypeFilter == 2 && bz.ItemInstance.Item.EquipmentSlot == EquipmentType.Ring
                                || SubTypeFilter == 1 && bz.ItemInstance.Item.EquipmentSlot == EquipmentType.Necklace
                                || SubTypeFilter == 5 && bz.ItemInstance.Item.EquipmentSlot == EquipmentType.Amulet
                                || SubTypeFilter == 3 && bz.ItemInstance.Item.EquipmentSlot == EquipmentType.Bracelet
                                || SubTypeFilter == 4 && (bz.ItemInstance.Item.EquipmentSlot == EquipmentType.Fairy
                                || bz.ItemInstance.Item.ItemType == ItemType.Box && bz.ItemInstance.Item.ItemSubType == 5))
                            {
                                applyLevelFilter = true;
                            }
                        }
                        break;

                    case BazaarListType.Specialist:
                        PocketType = PocketType.Equipment;
                        if (bz.ItemInstance.Item.ItemType == ItemType.Box && bz.ItemInstance.Item.ItemSubType == 2)
                        {
                            if (bz.Item is BoxInstance boxInstance)
                            {
                                if (SubTypeFilter == 0)
                                {
                                    if (LevelFilter == 0 || ((BoxInstance)bz.Item).SpLevel < LevelFilter * 10 + 1 && ((BoxInstance)bz.Item).SpLevel >= LevelFilter * 10 - 9)
                                    {
                                        if (UpgradeFilter == 0 || UpgradeFilter == bz.Item.Upgrade + 1)
                                        {
                                            if (SubTypeFilter == 0 || SubTypeFilter == 1 && ((BoxInstance)bz.Item).HoldingVNum == 0 || SubTypeFilter == 2 && ((BoxInstance)bz.Item).HoldingVNum != 0)
                                            {
                                                bzlist.Add(bz);
                                            }
                                        }
                                    }
                                }
                                else if (boxInstance.HoldingVNum == 0)
                                {
                                    if (SubTypeFilter == 1)
                                    {
                                        if (LevelFilter == 0 || ((BoxInstance)bz.Item).SpLevel < LevelFilter * 10 + 1 && ((BoxInstance)bz.Item).SpLevel >= LevelFilter * 10 - 9)
                                        {
                                            if (UpgradeFilter == 0 || UpgradeFilter == bz.Item.Upgrade + 1)
                                            {
                                                if (SubTypeFilter == 0 || SubTypeFilter == 1 && ((BoxInstance)bz.Item).HoldingVNum == 0 || SubTypeFilter == 2 && ((BoxInstance)bz.Item).HoldingVNum != 0)
                                                {
                                                    bzlist.Add(bz);
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (SubTypeFilter == 2 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 10
                                         || SubTypeFilter == 3 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 11
                                         || SubTypeFilter == 4 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 2
                                         || SubTypeFilter == 5 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 3
                                         || SubTypeFilter == 6 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 13
                                         || SubTypeFilter == 7 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 5
                                         || SubTypeFilter == 8 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 12
                                         || SubTypeFilter == 9 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 4
                                         || SubTypeFilter == 10 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 7
                                         || SubTypeFilter == 11 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 15
                                         || SubTypeFilter == 12 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 6
                                         || SubTypeFilter == 13 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 14
                                         || SubTypeFilter == 14 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 9
                                         || SubTypeFilter == 15 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 8
                                         || SubTypeFilter == 16 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 1
                                         || SubTypeFilter == 17 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 16
                                         || SubTypeFilter == 18 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 17
                                         || SubTypeFilter == 19 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 18
                                         || SubTypeFilter == 20 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 19
                                         || SubTypeFilter == 21 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 20
                                         || SubTypeFilter == 22 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 21
                                         || SubTypeFilter == 23 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 22
                                         || SubTypeFilter == 24 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 23
                                         || SubTypeFilter == 25 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 24
                                         || SubTypeFilter == 26 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 25
                                         || SubTypeFilter == 27 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 26
                                         || SubTypeFilter == 28 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 27
                                         || SubTypeFilter == 29 && ServerManager.Instance.GetItem(boxInstance.HoldingVNum).Morph == 28)
                                {
                                    if (LevelFilter == 0 || ((BoxInstance)bz.Item).SpLevel < LevelFilter * 10 + 1 && ((BoxInstance)bz.Item).SpLevel >= LevelFilter * 10 - 9)
                                    {
                                        if (UpgradeFilter == 0 || UpgradeFilter == bz.Item.Upgrade + 1)
                                        {
                                            if (SubTypeFilter == 0 || SubTypeFilter == 1 && ((BoxInstance)bz.Item).HoldingVNum == 0 || SubTypeFilter >= 2 && ((BoxInstance)bz.Item).HoldingVNum != 0)
                                            {
                                                bzlist.Add(bz);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        break;

                    case BazaarListType.Pet:
                        PocketType = PocketType.Equipment;

                        if (bz.ItemInstance.Item.ItemType == ItemType.Box && bz.ItemInstance.Item.ItemSubType == 0)
                        {
                            if (bz.Item is BoxInstance boxinstanced && (LevelFilter == 0 || boxinstanced.SpLevel < LevelFilter * 10 + 1 && boxinstanced.SpLevel >= LevelFilter * 10 - 9))
                            {
                                if (SubTypeFilter == 0 || SubTypeFilter == 1 && ((BoxInstance)bz.Item).HoldingVNum == 0 || SubTypeFilter == 2 && ((BoxInstance)bz.Item).HoldingVNum != 0)
                                {
                                    bzlist.Add(bz);
                                }
                            }
                        }

                        break;

                    case BazaarListType.Npc:
                        PocketType = PocketType.Equipment;
                        if (bz.ItemInstance.Item.ItemType == ItemType.Box && bz.ItemInstance.Item.ItemSubType == 1)
                        {
                            if (bz.Item is BoxInstance box && (LevelFilter == 0 || box.SpLevel < LevelFilter * 10 + 1 && box.SpLevel >= LevelFilter * 10 - 9))
                            {
                                if (SubTypeFilter == 0 || SubTypeFilter == 1 && ((BoxInstance)bz.Item).HoldingVNum == 0 || SubTypeFilter == 2 && ((BoxInstance)bz.Item).HoldingVNum != 0)
                                {
                                    bzlist.Add(bz);
                                }
                            }
                        }

                        break;

                    case BazaarListType.Shell:
                        PocketType = PocketType.Equipment;
                        if (bz.ItemInstance.Item.ItemType == ItemType.Shell)
                        {
                            if (SubTypeFilter == 0 || bz.ItemInstance.Item.ItemSubType == bz.ItemInstance.Item.ItemSubType + 1)
                            {
                                applyRareFilter = true;
                                if (bz.Item is BoxInstance box && (LevelFilter == 0 || box.SpLevel < LevelFilter * 10 + 1 && box.SpLevel >= LevelFilter * 10 - 9))
                                {
                                    bzlist.Add(bz);
                                }

                            }
                        }

                        break;

                    case BazaarListType.Main:
                        if (bz.ItemInstance.Item.Type == PocketType.Main)
                        {
                            if (SubTypeFilter == 0 || SubTypeFilter == 1 && bz.ItemInstance.Item.ItemType == ItemType.Main
                                || SubTypeFilter == 2 && bz.ItemInstance.Item.ItemType == ItemType.Upgrade
                                || SubTypeFilter == 3 && bz.ItemInstance.Item.ItemType == ItemType.Production
                                || SubTypeFilter == 4 && bz.ItemInstance.Item.ItemType == ItemType.Special
                                || SubTypeFilter == 5 && bz.ItemInstance.Item.ItemType == ItemType.Potion
                                || SubTypeFilter == 6 && bz.ItemInstance.Item.ItemType == ItemType.Event)
                            {
                                bzlist.Add(bz);
                            }
                        }
                        break;

                    case BazaarListType.Usable:
                        if (bz.ItemInstance.Item.Type == PocketType.Etc)
                        {
                            if (SubTypeFilter == 0
                                || SubTypeFilter == 1 && bz.ItemInstance.Item.ItemType == ItemType.Food
                                || SubTypeFilter == 2 && bz.ItemInstance.Item.ItemType == ItemType.Snack
                                || SubTypeFilter == 3 && bz.ItemInstance.Item.ItemType == ItemType.Magical
                                || SubTypeFilter == 4 && bz.ItemInstance.Item.ItemType == ItemType.Part
                                || SubTypeFilter == 5 && bz.ItemInstance.Item.ItemType == ItemType.Teacher
                                || SubTypeFilter == 6 && bz.ItemInstance.Item.ItemType == ItemType.Sell)
                            {
                                bzlist.Add(bz);
                            }
                        }
                        break;

                    case BazaarListType.Other:
                        PocketType = PocketType.Equipment;
                        if (bz.ItemInstance.Item.ItemType == ItemType.Box && !bz.ItemInstance.Item.Flag9)
                        {
                            bzlist.Add(bz);
                        }

                        break;

                    case BazaarListType.Vehicle:
                        if (bz.ItemInstance.Item.ItemType == ItemType.Box && bz.ItemInstance.Item.ItemSubType == 4)
                        {
                            if (bz.Item is BoxInstance box && (SubTypeFilter == 0 || SubTypeFilter == 1 && box.HoldingVNum == 0 || SubTypeFilter == 2 && box.HoldingVNum != 0))
                            {
                                bzlist.Add(bz);
                            }
                        }
                        break;

                    default:
                        bzlist.Add(bz);
                        break;
                }
            }

            return bzlist;
        }

        [HttpPost]
        public LanguageKey AddBazaar([FromBody] BazaarRequest bazaarRequest)
        {
            var items = _bazaarItemDao.Where(o => o.SellerId == bazaarRequest.CharacterId);
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
                item.Amount -= bazaarRequest.Amount;
                _itemInstanceDao.InsertOrUpdate(ref item);
                item.Id = Guid.NewGuid();
            }

            _itemInstanceDao.InsertOrUpdate(ref item);

            var bazaarItem = new BazaarItemDto
            {
                Amount = bazaarRequest.Amount,
                DateStart = DateTime.Now,
                Duration = bazaarRequest.Duration,
                IsPackage = bazaarRequest.IsPackage,
                MedalUsed = bazaarRequest.HasMedal,
                Price = bazaarRequest.Price,
                SellerId = bazaarRequest.CharacterId,
                ItemInstanceId = itemId
            };

            _bazaarItemDao.InsertOrUpdate(ref bazaarItem);
            return LanguageKey.OBJECT_IN_BAZAAR;
        }
    }
}
