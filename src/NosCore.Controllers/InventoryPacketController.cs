using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
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

        public void MoveEquipment(MvePacket mvePacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            var inv = Session.Character.Inventory.MoveInPocket(mvePacket.Slot, mvePacket.InventoryType, mvePacket.DestinationInventoryType, mvePacket.DestinationSlot, false);
            if (inv == null)
            {
                return;
            }
            Session.SendPacket(inv.GeneratePocketChange(inv.Type, inv.Slot));
            Session.SendPacket(((ItemInstance)null).GeneratePocketChange(mvePacket.InventoryType, mvePacket.Slot));
        }

        public void AskToDelete(BIPacket bIPacket)
        {
            switch (bIPacket.Option)
            {
                case null:
                    Session.SendPacket(
                        new DlgPacket
                        {
                            YesPacket = new BIPacket() { PocketType = bIPacket.PocketType, Slot = bIPacket.Slot, Option = 1 },
                            NoPacket = new BIPacket() { PocketType = bIPacket.PocketType, Slot = bIPacket.Slot, Option = 5 },
                            Question = Language.Instance.GetMessageFromKey(LanguageKey.ASK_TO_DELETE,
                                Session.Account.Language)
                        });
                    break;

                case 1:
                    Session.SendPacket(
                        new DlgPacket
                        {
                            YesPacket = new BIPacket() { PocketType = bIPacket.PocketType, Slot = bIPacket.Slot, Option = 2 },
                            NoPacket = new BIPacket() { PocketType = bIPacket.PocketType, Slot = bIPacket.Slot, Option = 5 },
                            Question = Language.Instance.GetMessageFromKey(LanguageKey.SURE_TO_DELETE,
                                Session.Account.Language)
                        });
                    break;

                case 2:
                    if (Session.Character.InExchangeOrTrade || bIPacket.PocketType == PocketType.Bazaar)
                    {
                        return;
                    }
                    Session.Character.DeleteItem(bIPacket.PocketType, bIPacket.Slot);
                    break;
            }
        }
    }
}