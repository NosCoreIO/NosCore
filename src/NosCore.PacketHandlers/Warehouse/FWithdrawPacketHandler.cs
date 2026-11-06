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
    public class FWithdrawPacketHandler(IFamilyWarehouseService familyWarehouseService)
        : PacketHandler<FWithdrawPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(FWithdrawPacket packet, ClientSession clientSession) =>
            familyWarehouseService.WithdrawAsync(clientSession, packet.Slot, packet.Amount);
    }
}
