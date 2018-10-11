using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using ItemInstance = NosCore.GameObject.Services.ItemBuilder.Item.ItemInstance;

namespace NosCore.Controllers
{
    public class InventoryPacketController : PacketController
    {
        private readonly WorldConfiguration _worldConfiguration;
        private readonly List<Item> _items;
        private readonly IItemBuilderService _itemBuilderService;

        [UsedImplicitly]
        public InventoryPacketController()
        {
        }

        public InventoryPacketController(WorldConfiguration worldConfiguration, List<Item> items, IItemBuilderService itemBuilderService)
        {
            _itemBuilderService = itemBuilderService;
            _worldConfiguration = worldConfiguration;
            _items = items;
        }

        [UsedImplicitly]
        public void MoveEquipment(MvePacket mvePacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            var inv = Session.Character.Inventory.MoveInPocket(mvePacket.Slot, mvePacket.InventoryType, mvePacket.DestinationInventoryType, mvePacket.DestinationSlot, false);
            Session.SendPacket(inv.GeneratePocketChange(mvePacket.DestinationInventoryType, mvePacket.DestinationSlot));
            Session.SendPacket(((ItemInstance)null).GeneratePocketChange(mvePacket.InventoryType, mvePacket.Slot));
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
            Session.Character.Inventory.MoveItem(mviPacket.InventoryType, mviPacket.Slot, mviPacket.Amount, mviPacket.DestinationSlot, out var previousInventory, out var newInventory);
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
                        canpick = PathFinder.Heuristic.Octile(Math.Abs(Session.Character.PositionX - mapItem.PositionX), Math.Abs(Session.Character.PositionY - mapItem.PositionY)) < 8;
                        break;

                    case PickerType.Mate:

                        break;
                }

                if (!canpick)
                {
                    return;
                }
                ItemInstance mapItemInstance = _itemBuilderService.Create( mapItem.VNum, mapItem.OwnerId ?? Session.Character.CharacterId, mapItem.Amount);
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
                            Session.SendPacket(new MsgPacket() { Message = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SP_POINTSADDED, Session.Account.Language), mapItemInstance.Item.EffectValue), Type = 0 });
                            Session.SendPacket(Session.Character.GenerateSpPoint());
                        }
                        Session.Character.MapInstance.DroppedList.TryRemove(getPacket.VisualId, out _);
                        Session.Character.MapInstance.Broadcast(Session.Character.GenerateGet(getPacket.VisualId));
                    }
                    else
                    {
                        var amount = mapItem.Amount;
                        var inv = Session.Character.Inventory.AddItemToPocket(mapItemInstance).FirstOrDefault();

                        if (inv != null)
                        {
                            Session.SendPacket(inv.GeneratePocketChange(inv.Type, inv.Slot));
                            Session.Character.MapInstance.DroppedList.TryRemove(getPacket.VisualId, out var value);
                            Session.Character.MapInstance.Broadcast(Session.Character.GenerateGet(getPacket.VisualId));
                            if (getPacket.PickerType == PickerType.Mate)
                            {
                                Session.SendPacket(Session.Character.GenerateIcon(1, inv.ItemVNum));
                            }
                            Session.SendPacket(Session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, Session.Account.Language)}: {inv.Item.Name} x {amount}", SayColorType.Green));
                            if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.LodInstance)
                            {
                                Session.Character.MapInstance.Broadcast(Session.Character.GenerateSay($"{string.Format(Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED_LOD, Session.Account.Language), Session.Character.Name)}: {inv.Item.Name} x {mapItem.Amount}", SayColorType.Yellow));
                            }
                        }
                        else
                        {
                            Session.SendPacket(new MsgPacket() { Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE, Session.Account.Language), Type = 0 });
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
                        Session.SendPacket(Session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, Session.Account.Language)}: {mapItemInstance.Item.Name} x {mapItem.Amount}", SayColorType.Green));
                    }
                    else
                    {
                        Session.Character.Gold = maxGold;
                        Session.SendPacket(new MsgPacket() { Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD, Session.Account.Language), Type = 0 });
                    }
                    Session.SendPacket(Session.Character.GenerateGold());
                    Session.Character.MapInstance.DroppedList.TryRemove(getPacket.VisualId, out _);
                    Session.Character.MapInstance.Broadcast(Session.Character.GenerateGet(getPacket.VisualId));
                }
            }
        }

        [UsedImplicitly]
        public void PutItem(PutPacket putPacket)
        {
            lock (Session.Character.Inventory)
            {
                var invitem = Session.Character.Inventory.LoadBySlotAndType<ItemInstance>(putPacket.Slot, putPacket.PocketType);
                if (invitem?.Item.IsDroppable == true && invitem.Item.IsTradable && !Session.Character.InExchangeOrTrade)
                {
                    if (putPacket.Amount > 0 && putPacket.Amount <= _worldConfiguration.MaxItemAmount)
                    {
                        if (Session.Character.MapInstance.DroppedList.Count < 200)
                        {
                            var droppedItem = Session.Character.MapInstance.PutItem(putPacket.Amount, ref invitem, Session);
                            if (droppedItem == null)
                            {
                                Session.SendPacket(new MsgPacket() { Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE, Session.Account.Language), Type = 0 });
                                return;
                            }

                            if (droppedItem.Amount == 0)
                            {
                                Session.Character.Inventory.DeleteFromTypeAndSlot(invitem.Type, invitem.Slot);
                            }

                            Session.SendPacket(invitem.GeneratePocketChange(invitem.Type, invitem.Slot));

                            Session.Character.MapInstance.Broadcast(droppedItem.GenerateDrop());
                        }
                        else
                        {
                            Session.SendPacket(new MsgPacket() { Message = Language.Instance.GetMessageFromKey(LanguageKey.DROP_MAP_FULL, Session.Account.Language), Type = 0 });
                        }
                    }
                    else
                    {
                        Session.SendPacket(new MsgPacket() { Message = Language.Instance.GetMessageFromKey(LanguageKey.BAD_DROP_AMOUNT, Session.Account.Language), Type = 0 });
                    }
                }
                else
                {
                    Session.SendPacket(new MsgPacket() { Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE, Session.Account.Language), Type = 0 });
                }
            }
        }

        public void AskToDelete(BIPacket bIPacket)
        {
            switch (bIPacket.Option)
            {
                case null:
                    Session.SendPacket(
                        new DlgPacket
                        {
                            YesPacket = new BIPacket() { PocketType = bIPacket.PocketType, Slot = bIPacket.Slot, Option = RequestDeletionType.Requested },
                            NoPacket = new BIPacket() { PocketType = bIPacket.PocketType, Slot = bIPacket.Slot, Option = RequestDeletionType.Declined },
                            Question = Language.Instance.GetMessageFromKey(LanguageKey.ASK_TO_DELETE,
                                Session.Account.Language)
                        });
                    break;

                case RequestDeletionType.Requested:
                    Session.SendPacket(
                        new DlgPacket
                        {
                            YesPacket = new BIPacket() { PocketType = bIPacket.PocketType, Slot = bIPacket.Slot, Option = RequestDeletionType.Confirmed },
                            NoPacket = new BIPacket() { PocketType = bIPacket.PocketType, Slot = bIPacket.Slot, Option = RequestDeletionType.Declined },
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
            }
        }
    }
}