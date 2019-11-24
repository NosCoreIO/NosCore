using System.Collections.Generic;
using ChickenAPI.Packets.ClientPackets.Player;
using ChickenAPI.Packets.Enumerations;
using NosCore.Data;
using NosCore.GameObject;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.UpgradeService;

namespace NosCore.PacketHandlers.Inventory
{
    public class UpgradePacketHandler : PacketHandler<UpgradePacket>, IWorldPacketHandler
    {
        private readonly IUpgradeService _upgradeService;

        public UpgradePacketHandler(IUpgradeService upgradeService)
        {
            _upgradeService = upgradeService;
        }

        public override void Execute(UpgradePacket packet, ClientSession clientSession)
        {
            InventoryItemInstance item1 = null;
            InventoryItemInstance item2 = null;
            if (packet.UpgradeType == UpgradePacketType.SumResistance)
            {
                var acceptedItemType = new List<EquipmentType> { EquipmentType.Gloves, EquipmentType.Boots };

                item1 = clientSession.Character.Inventory.LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
                if (!(item1?.ItemInstance is WearableInstance))
                {
                    return;
                }

                if (packet.Slot2 == null || packet.InventoryType2 == null)
                {
                    return;
                }

                item2 = clientSession.Character.Inventory.LoadBySlotAndType((byte)packet.Slot2, (NoscorePocketType)packet.InventoryType2);
                if (!(item2?.ItemInstance is WearableInstance))
                {
                    return;
                }

                if (item1.ItemInstance.Upgrade + item2.ItemInstance.Upgrade > UpgradeHelper.Instance.MaxSumLevel)
                {
                    return;
                }

                if (!acceptedItemType.Contains(item1.ItemInstance.Item.EquipmentSlot) ||
                    !acceptedItemType.Contains(item2.ItemInstance.Item.EquipmentSlot))
                {
                    return;
                }

            }
            _upgradeService.HandlePacket(packet.UpgradeType, clientSession, item1, item2);
        }
    }
}
