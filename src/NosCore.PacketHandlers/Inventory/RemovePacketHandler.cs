//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;


namespace NosCore.PacketHandlers.Inventory
{
    public class RemovePacketHandler : PacketHandler<RemovePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(RemovePacket removePacket, ClientSession clientSession)
        {
            var inventory =
                clientSession.Character.InventoryService.LoadBySlotAndType((short)removePacket.InventorySlot,
                    NoscorePocketType.Wear);
            if (inventory == null)
            {
                return;
            }

            if (inventory.ItemInstance.Item.EquipmentSlot == EquipmentType.Sp)
            {
                await clientSession.HandlePacketsAsync(new[] { new SpTransformPacket
                {
                    Type = SlPacketType.WearSpAndTransform
                } });
            }

            var inv = clientSession.Character.InventoryService.MoveInPocket((short)removePacket.InventorySlot,
                NoscorePocketType.Wear, NoscorePocketType.Equipment);

            if (inv == null)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpace
                });
                return;
            }

            await clientSession.SendPacketAsync(inv.GeneratePocketChange((PocketType)inv.Type, inv.Slot));

            await clientSession.Character.MapInstance.SendPacketAsync(clientSession.Character.GenerateEq());
            await clientSession.SendPacketAsync(clientSession.Character.GenerateEquipment());

            if (inv.ItemInstance.Item.EquipmentSlot == EquipmentType.Fairy)
            {
                await clientSession.Character.MapInstance.SendPacketAsync(
                    clientSession.Character.GeneratePairy(null));
            }
        }
    }
}
