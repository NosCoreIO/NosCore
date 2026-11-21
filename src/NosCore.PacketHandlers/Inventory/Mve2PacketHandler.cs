//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;

namespace NosCore.PacketHandlers.Inventory
{
    public class Mve2PacketHandler : PacketHandler<Mve2Packet>, IWorldPacketHandler
    {
        private static readonly HashSet<NoscorePocketType> Bags =
        [
            NoscorePocketType.Equipment, NoscorePocketType.Main, NoscorePocketType.Etc,
            NoscorePocketType.Specialist, NoscorePocketType.Costume, NoscorePocketType.Mount
        ];

        public override async Task ExecuteAsync(Mve2Packet packet, ClientSession clientSession)
        {
            var source = packet.InventoryType.ToNoscorePocketType();
            var destination = packet.DestinationInventoryType.ToNoscorePocketType();
            if (!Bags.Contains(source) || !Bags.Contains(destination))
            {
                return;
            }

            var inventory = clientSession.Character.InventoryService;
            inventory.MoveInPocket(packet.Slot, source, destination, packet.DestinationSlot, false);
            await clientSession.SendPacketAsync(inventory.LoadBySlotAndType(packet.Slot, source)
                .GeneratePocketChange(packet.InventoryType, packet.Slot));
            if (packet.DestinationSlot < inventory.GetMaxSlots(destination))
            {
                await clientSession.SendPacketAsync(inventory.LoadBySlotAndType(packet.DestinationSlot, destination)
                    .GeneratePocketChange(packet.DestinationInventoryType, packet.DestinationSlot));
            }
        }
    }
}
