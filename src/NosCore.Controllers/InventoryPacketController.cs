using System.Collections.Generic;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;

namespace NosCore.Controllers
{
    public class InventoryPacketController : PacketController
    {
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

        public void PutItem(PutPacket putPacket)
        {
            lock (Session.Character.Inventory)
            {
                ItemInstance invitem = Session.Character.Inventory.LoadBySlotAndType<ItemInstance>(putPacket.Slot, putPacket.PocketType);
                if (invitem != null && invitem.Item.IsDroppable && invitem.Item.IsTradable && !Session.Character.InExchangeOrTrade)
                {
                    if (putPacket.Amount > 0 && putPacket.Amount <= _worldConfiguration.MaxItemAmount)
                    {
                        if (Session.Character.MapInstance.DroppedList.Count < 200)
                        {
                            MapItem droppedItem = Session.Character.MapInstance.PutItem(putPacket.PocketType, putPacket.Slot, putPacket.Amount, ref invitem, Session);
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