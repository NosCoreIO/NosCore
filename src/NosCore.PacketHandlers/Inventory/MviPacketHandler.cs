//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Inventory;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Inventory
{
    public class MviPacketHandler : PacketHandler<MviPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(MviPacket mviPacket, ClientSession clientSession)
        {
            // actually move the item from source to destination
            clientSession.Character.InventoryService.TryMoveItem((NoscorePocketType)mviPacket.InventoryType, mviPacket.Slot,
                mviPacket.Amount,
                mviPacket.DestinationSlot, out var previousInventory, out var newInventory);
            await clientSession.SendPacketAsync(
                newInventory.GeneratePocketChange(mviPacket.InventoryType, mviPacket.DestinationSlot));
            await clientSession.SendPacketAsync(previousInventory.GeneratePocketChange(mviPacket.InventoryType, mviPacket.Slot));
        }
    }
}
