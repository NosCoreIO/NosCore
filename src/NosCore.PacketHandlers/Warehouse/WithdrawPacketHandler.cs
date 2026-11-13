//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Packets.ClientPackets.Warehouse;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Warehouse
{
    public class WithdrawPacketHandler(IPersonalWarehouseService warehouseService) : PacketHandler<WithdrawPacket>,
        IWorldPacketHandler
    {
        public override Task ExecuteAsync(WithdrawPacket packet, ClientSession clientSession) =>
            warehouseService.WithdrawAsync(clientSession,
                packet.PetBackpack ? WarehouseType.PetWarehouse : WarehouseType.Warehouse, packet.Slot, packet.Amount);
    }
}
