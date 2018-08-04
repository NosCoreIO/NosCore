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

        [UsedImplicitly]
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
                            NoPacket = new BIPacket() { PocketType = bIPacket.PocketType, Slot = bIPacket.Slot, Option = RequestDeletionType.Declined  },
                            Question = Language.Instance.GetMessageFromKey(LanguageKey.SURE_TO_DELETE,
                                Session.Account.Language)
                        });
                    break;

                case RequestDeletionType.Confirmed:
                    if (Session.Character.InExchangeOrTrade)
                    {
                        return;
                    }
                    Session.Character.DeleteItem(bIPacket.PocketType, bIPacket.Slot);
                    break;
            }
        }
    }
}