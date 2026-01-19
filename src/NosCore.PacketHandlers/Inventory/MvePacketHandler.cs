//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Inventory
{
    public class MvePacketHandler : PacketHandler<MvePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(MvePacket mvePacket, ClientSession clientSession)
        {
            var inv = clientSession.Character.InventoryService.MoveInPocket(mvePacket.Slot,
                (NoscorePocketType)mvePacket.InventoryType,
                (NoscorePocketType)mvePacket.DestinationInventoryType, mvePacket.DestinationSlot, false);
            await clientSession.SendPacketAsync(inv.GeneratePocketChange(mvePacket.DestinationInventoryType,
                mvePacket.DestinationSlot));
            await clientSession.SendPacketAsync(
                ((InventoryItemInstance?)null).GeneratePocketChange(mvePacket.InventoryType, mvePacket.Slot));
        }
    }
}
