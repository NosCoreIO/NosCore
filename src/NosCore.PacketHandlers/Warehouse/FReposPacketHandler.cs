//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Warehouse;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Warehouse
{
    public class FReposPacketHandler(IFamilyWarehouseService familyWarehouseService)
        : PacketHandler<FReposPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(FReposPacket packet, ClientSession clientSession) =>
            familyWarehouseService.MoveAsync(clientSession, packet.OldSlot, packet.Amount, packet.NewSlot);
    }
}
