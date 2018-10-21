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
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.PathFinder;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.Controllers
{
    public class InventoryPacketController : PacketController
    {
        private readonly IItemBuilderService _itemBuilderService;
        private readonly WorldConfiguration _worldConfiguration;

        [UsedImplicitly]
        public InventoryPacketController()
        {
        }

        public InventoryPacketController(WorldConfiguration worldConfiguration,
            IItemBuilderService itemBuilderService)
        {
            _itemBuilderService = itemBuilderService;
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
            Session.SendPacket(((ItemInstance) null).GeneratePocketChange(mvePacket.InventoryType, mvePacket.Slot));
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

        [UsedImplicitly]
        public void GetItem(GetPacket getPacket)
        {
            if (getPacket.VisualId < 100000)
            {
                //TODO buttons
            }
            else
            {
                if (!Session.Character.MapInstance.DroppedList.ContainsKey(getPacket.VisualId))
                {
                    return;
                }

                var mapItem = Session.Character.MapInstance.DroppedList[getPacket.VisualId];

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
                        Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.UNKNOWN_PICKERTYPE));
                        return;
                }

                if (!canpick)
                {
                    return;
                }

                ItemInstance mapItemInstance = _itemBuilderService.Create(mapItem.VNum,
                    mapItem.OwnerId ?? Session.Character.CharacterId, mapItem.Amount);
                //TODO not your item
                if (mapItem.VNum != 1046)
                {
                    if (mapItemInstance.Item.ItemType == ItemType.Map)
                    {
                        if (mapItemInstance.Item.Effect == 71)
                        {
                            Session.Character.SpPoint += mapItemInstance.Item.EffectValue;
                            if (Session.Character.SpPoint > 10000)
                            {
                                Session.Character.SpPoint = 10000;
                            }

                            Session.SendPacket(new MsgPacket
                            {
                                Message = string.Format(
                                    Language.Instance.GetMessageFromKey(LanguageKey.SP_POINTSADDED,
                                        Session.Account.Language), mapItemInstance.Item.EffectValue),
                                Type = 0
                            });
                            Session.SendPacket(Session.Character.GenerateSpPoint());
                        }

                        Session.Character.MapInstance.DroppedList.TryRemove(getPacket.VisualId, out _);
                        Session.Character.MapInstance.Sessions.SendPacket(Session.Character.GenerateGet(getPacket.VisualId));
                    }
                    else
                    {
                        var amount = mapItem.Amount;
                        var inv = Session.Character.Inventory.AddItemToPocket(mapItemInstance).FirstOrDefault();

                        if (inv != null)
                        {
                            Session.SendPacket(inv.GeneratePocketChange(inv.Type, inv.Slot));
                            Session.Character.MapInstance.DroppedList.TryRemove(getPacket.VisualId, out var value);
                            Session.Character.MapInstance.Sessions.SendPacket(Session.Character.GenerateGet(getPacket.VisualId));
                            if (getPacket.PickerType == PickerType.Mate)
                            {
                                Session.SendPacket(Session.Character.GenerateIcon(1, inv.ItemVNum));
                            }

                            Session.SendPacket(Session.Character.GenerateSay(
                                $"{Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, Session.Account.Language)}: {inv.Item.Name} x {amount}",
                                SayColorType.Green));
                            if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.LodInstance)
                            {
                                var name = string.Format(
                                    Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED_LOD,
                                        Session.Account.Language), Session.Character.Name);
                                Session.Character.MapInstance.Sessions.SendPacket(Session.Character.GenerateSay(
                                    $"{name}: {inv.Item.Name} x {mapItem.Amount}",
                                    SayColorType.Yellow));
                            }
                        }
                        else
                        {
                            Session.SendPacket(new MsgPacket
                            {
                                Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                                    Session.Account.Language),
                                Type = 0
                            });
                        }
                    }
                }
                else
                {
                    // handle gold drop
                    var maxGold = _worldConfiguration.MaxGoldAmount;
                    if (Session.Character.Gold + mapItem.Amount <= maxGold)
                    {
                        if (getPacket.PickerType == PickerType.Mate)
                        {
                            Session.SendPacket(Session.Character.GenerateIcon(1, mapItem.VNum));
                        }

                        Session.Character.Gold += mapItem.Amount;
                        Session.SendPacket(Session.Character.GenerateSay(
                            $"{Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, Session.Account.Language)}: {mapItemInstance.Item.Name} x {mapItem.Amount}",
                            SayColorType.Green));
                    }
                    else
                    {
                        Session.Character.Gold = maxGold;
                        Session.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD,
                                Session.Account.Language),
                            Type = 0
                        });
                    }

                    Session.SendPacket(Session.Character.GenerateGold());
                    Session.Character.MapInstance.DroppedList.TryRemove(getPacket.VisualId, out _);
                    Session.Character.MapInstance.Sessions.SendPacket(Session.Character.GenerateGet(getPacket.VisualId));
                }
            }
        }

        [UsedImplicitly]
        public void PutItem(PutPacket putPacket)
        {
            lock (Session.Character.Inventory)
            {
                var invitem =
                    Session.Character.Inventory.LoadBySlotAndType<ItemInstance>(putPacket.Slot, putPacket.PocketType);
                if ((invitem?.Item.IsDroppable ?? false) && invitem.Item.IsTradable
                    && !Session.Character.InExchangeOrTrade)
                {
                    if (putPacket.Amount > 0 && putPacket.Amount <= _worldConfiguration.MaxItemAmount)
                    {
                        if (Session.Character.MapInstance.DroppedList.Count < 200)
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

                            if (droppedItem.Amount == 0)
                            {
                                Session.Character.Inventory.DeleteFromTypeAndSlot(invitem.Type, invitem.Slot);
                            }

                            Session.SendPacket(invitem.GeneratePocketChange(invitem.Type, invitem.Slot));

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
                                PocketType = bIPacket.PocketType, Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Requested
                            },
                            NoPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType, Slot = bIPacket.Slot,
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
                                PocketType = bIPacket.PocketType, Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Confirmed
                            },
                            NoPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType, Slot = bIPacket.Slot,
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