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

using System;
using System.Linq;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.GameObject.Services.MapItemBuilder;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.PathFinder;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Group;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Controllers
{
    public class InventoryPacketController : PacketController
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly WorldConfiguration _worldConfiguration;

        [UsedImplicitly]
        public InventoryPacketController()
        {
        }

        public InventoryPacketController(WorldConfiguration worldConfiguration)
        {
            _worldConfiguration = worldConfiguration;
        }

        [UsedImplicitly]
        public void MoveEquipment(MvePacket mvePacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            var inv = Session.Character.Inventory.MoveInPocket(mvePacket.Slot, mvePacket.InventoryType,
                mvePacket.DestinationInventoryType, mvePacket.DestinationSlot, false);
            Session.SendPacket(inv.GeneratePocketChange(mvePacket.DestinationInventoryType, mvePacket.DestinationSlot));
            Session.SendPacket(((IItemInstance)null).GeneratePocketChange(mvePacket.InventoryType, mvePacket.Slot));
        }

        [UsedImplicitly]
        public void MoveItem(MviPacket mviPacket)
        {
            // check if the character is allowed to move the item
            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            // actually move the item from source to destination
            Session.Character.Inventory.TryMoveItem(mviPacket.InventoryType, mviPacket.Slot, mviPacket.Amount,
                mviPacket.DestinationSlot, out var previousInventory, out var newInventory);
            Session.SendPacket(newInventory.GeneratePocketChange(mviPacket.InventoryType, mviPacket.DestinationSlot));
            Session.SendPacket(previousInventory.GeneratePocketChange(mviPacket.InventoryType, mviPacket.Slot));
        }


        /// <summary>
        /// remove packet
        /// </summary>
        /// <param name="removePacket"></param>
        public void Remove(RemovePacket removePacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            IItemInstance inventory = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>((short)removePacket.InventorySlot, PocketType.Wear);
            if (inventory == null)
            {
                return;
            }

            IItemInstance inv = Session.Character.Inventory.MoveInPocket((short)removePacket.InventorySlot, PocketType.Wear, PocketType.Equipment);

            if (inv == null)
            {
                Session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                    Session.Account.Language),
                    Type = 0
                });
                return;
            }
            Session.SendPacket(inv.GeneratePocketChange(inv.Type, inv.Slot));

            Session.Character.MapInstance.Sessions.SendPacket(Session.Character.GenerateEq());
            Session.SendPacket(Session.Character.GenerateEquipment());

            if (inv.Item.EquipmentSlot == EquipmentType.Fairy)
            {
                Session.Character.MapInstance.Sessions.SendPacket(Session.Character.GeneratePairy((WearableInstance)null));
            }
        }
        
        public void CreateShop(MShopPacket mShopPacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                //todo log
                return;
            }

            if (Session.Character.MapInstance.Portals.Any(por => Session.Character.PositionX < por.SourceX + 6 && Session.Character.PositionX > por.SourceX - 6 && Session.Character.PositionY < por.SourceY + 6 && Session.Character.PositionY > por.SourceY - 6))
            {
                Session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.SHOP_NEAR_PORTAL,
                        Session.Account.Language),
                    Type = 0
                });
                return;
            }

            if (Session.Character.Group != null && Session.Character.Group?.Type != GroupType.Group)
            {
                Session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.SHOP_NOT_ALLOWED_IN_RAID,
                        Session.Account.Language),
                    Type = 0
                });
                return;
            }

            if (!Session.Character.MapInstance.ShopAllowed)
            {
                Session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.SHOP_NOT_ALLOWED,
                        Session.Account.Language),
                    Type = 0
                });
                return;
            }

            switch (mShopPacket.Type)
            {
                case CreateShopPacketType.Open:
                    break;
                case CreateShopPacketType.Close:
                    Session.Character.CloseShop();
                    break;
                case CreateShopPacketType.Create:
                    Session.SendPacket(new IshopPacket());
                    break;
                default:
                    //todo log
                    return;
            }

            //if (typePacket == 0)
            //{
            //    if (Session.CurrentMapInstance.UserShops.Count(s => s.Value.OwnerId == Session.Character.CharacterId) != 0)
            //    {
            //        return;
            //    }
            //    MapShop myShop = new MapShop();

            //    if (packetsplit.Length > 82)
            //    {
            //        short shopSlot = 0;

            //        for (short j = 3, i = 0; j < 82; j += 4, i++)
            //        {
            //            Enum.TryParse(packetsplit[j], out type[i]);
            //            short.TryParse(packetsplit[j + 1], out slot[i]);
            //            byte.TryParse(packetsplit[j + 2], out qty[i]);

            //            long.TryParse(packetsplit[j + 3], out gold[i]);
            //            if (gold[i] < 0)
            //            {
            //                return;
            //            }
            //            if (qty[i] <= 0)
            //            {
            //                continue;
            //            }
            //            ItemInstance inv = Session.Character.Inventory.LoadBySlotAndType(slot[i], type[i]);
            //            if (inv == null)
            //            {
            //                continue;
            //            }
            //            if (inv.Amount < qty[i])
            //            {
            //                return;
            //            }
            //            if (!inv.Item.IsTradable || inv.IsBound)
            //            {
            //                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SHOP_ONLY_TRADABLE_ITEMS"), 10));
            //                Session.SendPacket("shop_end 0");
            //                return;
            //            }

            //            PersonalShopItem personalshopitem = new PersonalShopItem
            //            {
            //                ShopSlot = shopSlot,
            //                Price = gold[i],
            //                ItemInstance = inv,
            //                SellAmount = qty[i]
            //            };
            //            myShop.Items.Add(personalshopitem);
            //            shopSlot++;
            //        }
            //    }
            //    if (myShop.Items.Count != 0)
            //    {
            //        if (!myShop.Items.Any(s => !s.ItemInstance.Item.IsSoldable || s.ItemInstance.IsBound))
            //        {
            //            for (int i = 83; i < packetsplit.Length; i++)
            //            {
            //                shopname += $"{packetsplit[i]} ";
            //            }

            //            // trim shopname
            //            shopname = shopname.TrimEnd(' ');

            //            // create default shopname if it's empty
            //            if (string.IsNullOrWhiteSpace(shopname) || string.IsNullOrEmpty(shopname))
            //            {
            //                shopname = Language.Instance.GetMessageFromKey("SHOP_PRIVATE_SHOP");
            //            }

            //            // truncate the string to a max-length of 20
            //            shopname = shopname.Truncate(20);
            //            myShop.OwnerId = Session.Character.CharacterId;
            //            myShop.Name = shopname;
            //            Session.CurrentMapInstance.UserShops.Add(Session.CurrentMapInstance.LastUserShopId++, myShop);

            //            Session.Character.HasShopOpened = true;

            //            Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GeneratePlayerFlag(Session.CurrentMapInstance.LastUserShopId), ReceiverType.AllExceptMe);
            //            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateShop(shopname));
            //            Session.SendPacket(UserInterfaceHelper.Instance.GenerateInfo(Language.Instance.GetMessageFromKey("SHOP_OPEN")));

            //            Session.Character.IsSitting = true;
            //            Session.Character.IsShopping = true;

            //            Session.Character.LoadSpeed();
            //            Session.SendPacket(Session.Character.GenerateCond());
            //            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateRest());
            //        }
            //        else
            //        {
            //            Session.SendPacket("shop_end 0");
            //            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ITEM_NOT_SOLDABLE"), 10));
            //        }
            //    }
            //    else
            //    {
            //        Session.SendPacket("shop_end 0");
            //        Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SHOP_EMPTY"), 10));
            //    }
            //}
        }

        /// <summary>
        /// wear packet
        /// </summary>
        /// <param name="wearPacket"></param>
        [UsedImplicitly]
        public void Wear(WearPacket wearPacket)
        {
            UseItem(new UseItemPacket { Slot = wearPacket.InventorySlot, OriginalContent = wearPacket.OriginalContent, OriginalHeader = wearPacket.OriginalHeader, Type = wearPacket.Type });
        }

        /// <summary>
        /// u_i packet
        /// </summary>
        /// <param name="useItemPacket"></param>
        public void UseItem(UseItemPacket useItemPacket)
        {
            IItemInstance inv = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(useItemPacket.Slot, useItemPacket.Type);
            if (inv?.Requests == null)
            {
                return;
            }

            inv.Requests.OnNext(new RequestData<Tuple<IItemInstance, UseItemPacket>>(Session, new Tuple<IItemInstance, UseItemPacket>(inv, useItemPacket)));
        }

        /// <summary>
        /// sl packet
        /// </summary>
        /// <param name="spTransformPacket"></param>
        public void SpTransform(SpTransformPacket spTransformPacket)
        {
            SpecialistInstance specialistInstance = Session.Character.Inventory.LoadBySlotAndType<SpecialistInstance>((byte)EquipmentType.Sp, PocketType.Wear);

            if (spTransformPacket.Type == 10)
            {
                //TODO set points
            }
            else
            {
                if (Session.Character.IsSitting)
                {
                    return;
                }

                if (specialistInstance == null)
                {
                    Session.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.NO_SP, Session.Account.Language)
                    });

                    return;
                }

                if (Session.Character.IsVehicled)
                {
                    Session.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.REMOVE_VEHICLE, Session.Account.Language)
                    });
                    return;
                }

                double currentRunningSeconds = (SystemTime.Now() - Session.Character.LastSp).TotalSeconds;

                if (Session.Character.UseSp)
                {
                    Session.Character.LastSp = SystemTime.Now();
                    Session.Character.RemoveSp();
                }
                else
                {
                    if (Session.Character.SpPoint == 0 && Session.Character.SpAdditionPoint == 0)
                    {
                        Session.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.SP_NOPOINTS, Session.Account.Language)
                        });
                        return;
                    }
                    if (currentRunningSeconds >= Session.Character.SpCooldown)
                    {
                        if (spTransformPacket.Type == 1)
                        {
                            Session.Character.ChangeSp();
                        }
                        else
                        {
                            Session.SendPacket(new DelayPacket
                            {
                                Type = 3,
                                Delay = 5000,
                                Packet = new SpTransformPacket { Type = 1 }
                            });
                            Session.Character.MapInstance.Sessions.SendPacket(new GuriPacket
                            {
                                Type = 2,
                                Argument = 1,
                                VisualEntityId = Session.Character.CharacterId
                            });
                        }
                    }
                    else
                    {
                        Session.SendPacket(new MsgPacket
                        {
                            Message = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING,
                                Session.Account.Language), Session.Character.SpCooldown - (int)Math.Round(currentRunningSeconds))
                        });
                    }
                }
            }
        }

        [UsedImplicitly]
        public void GetItem(GetPacket getPacket)
        {
            if (!Session.Character.MapInstance.MapItems.ContainsKey(getPacket.VisualId))
            {
                return;
            }

            var mapItem = Session.Character.MapInstance.MapItems[getPacket.VisualId];

            var canpick = false;
            switch (getPacket.PickerType)
            {
                case PickerType.Character:
                    canpick = Heuristic.Octile(Math.Abs(Session.Character.PositionX - mapItem.PositionX),
                        Math.Abs(Session.Character.PositionY - mapItem.PositionY)) < 8;
                    break;

                case PickerType.Mate:
                    return;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.UNKNOWN_PICKERTYPE));
                    return;
            }

            if (!canpick)
            {
                return;
            }

            //TODO add group drops
            if (mapItem.OwnerId != null && mapItem.DroppedAt.AddSeconds(30) > SystemTime.Now() && mapItem.OwnerId != Session.Character.CharacterId)
            {
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.NOT_YOUR_ITEM, Session.Account.Language), SayColorType.Yellow));
                return;
            }
            mapItem.Requests.OnNext(new RequestData<Tuple<MapItem, GetPacket>>(Session, new Tuple<MapItem, GetPacket>(mapItem, getPacket)));
        }

        [UsedImplicitly]
        public void PutItem(PutPacket putPacket)
        {
            lock (Session.Character.Inventory)
            {
                var invitem =
                    Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(putPacket.Slot, putPacket.PocketType);
                if ((invitem?.Item.IsDroppable ?? false) && !Session.Character.InExchangeOrTrade)
                {
                    if (putPacket.Amount > 0 && putPacket.Amount <= _worldConfiguration.MaxItemAmount)
                    {
                        if (Session.Character.MapInstance.MapItems.Count < 200)
                        {
                            var droppedItem =
                                Session.Character.MapInstance.PutItem(putPacket.Amount, invitem, Session);
                            if (droppedItem == null)
                            {
                                Session.SendPacket(new MsgPacket
                                {
                                    Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE,
                                        Session.Account.Language),
                                    Type = 0
                                });
                                return;
                            }
                            invitem = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(putPacket.Slot, putPacket.PocketType);
                            Session.SendPacket(invitem.GeneratePocketChange(putPacket.PocketType, putPacket.Slot));
                            Session.Character.MapInstance.Sessions.SendPacket(droppedItem.GenerateDrop());
                        }
                        else
                        {
                            Session.SendPacket(new MsgPacket
                            {
                                Message = Language.Instance.GetMessageFromKey(LanguageKey.DROP_MAP_FULL,
                                    Session.Account.Language),
                                Type = 0
                            });
                        }
                    }
                    else
                    {
                        Session.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.BAD_DROP_AMOUNT,
                                Session.Account.Language),
                            Type = 0
                        });
                    }
                }
                else
                {
                    Session.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE,
                            Session.Account.Language),
                        Type = 0
                    });
                }
            }
        }

        public void AskToDelete(BiPacket bIPacket)
        {
            switch (bIPacket.Option)
            {
                case null:
                    Session.SendPacket(
                        new DlgPacket
                        {
                            YesPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Requested
                            },
                            NoPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Declined
                            },
                            Question = Language.Instance.GetMessageFromKey(LanguageKey.ASK_TO_DELETE,
                                Session.Account.Language)
                        });
                    break;

                case RequestDeletionType.Requested:
                    Session.SendPacket(
                        new DlgPacket
                        {
                            YesPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Confirmed
                            },
                            NoPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Declined
                            },
                            Question = Language.Instance.GetMessageFromKey(LanguageKey.SURE_TO_DELETE,
                                Session.Account.Language)
                        });
                    break;

                case RequestDeletionType.Confirmed:
                    if (Session.Character.InExchangeOrTrade)
                    {
                        return;
                    }

                    var item = Session.Character.Inventory.DeleteFromTypeAndSlot(bIPacket.PocketType, bIPacket.Slot);
                    Session.SendPacket(item.GeneratePocketChange(bIPacket.PocketType, bIPacket.Slot));
                    break;
                default:
                    return;
            }
        }
    }
}