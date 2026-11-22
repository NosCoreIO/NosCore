//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Event;

namespace NosCore.PacketHandlers.Inventory
{
    public class RaidopenPacketHandler : PacketHandler<RaidopenPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(RaidopenPacket packet, ClientSession clientSession)
        {
            return clientSession.SendPacketAsync(clientSession.Character.InventoryService.GeneratePocket(NoscorePocketType.Raid));
        }
    }
}
