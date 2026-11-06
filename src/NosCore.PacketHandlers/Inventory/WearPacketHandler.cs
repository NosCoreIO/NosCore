//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Inventory
{
    public class WearPacketHandler : PacketHandler<WearPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(WearPacket wearPacket, ClientSession clientSession)
        {
            if (wearPacket.PartnerSlot != 0)
            {
                return Task.CompletedTask;
            }

            return clientSession.HandlePacketsAsync(new[]
            {
                new UseItemPacket
                {
                    Slot = wearPacket.InventorySlot,
                    Type = PocketType.Equipment
                }
            });
        }
    }
}
