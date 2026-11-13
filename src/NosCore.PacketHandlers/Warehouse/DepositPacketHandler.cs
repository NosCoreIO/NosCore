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
    public class DepositPacketHandler(IPersonalWarehouseService warehouseService) : PacketHandler<DepositPacket>,
        IWorldPacketHandler
    {
        public override Task ExecuteAsync(DepositPacket packet, ClientSession clientSession) =>
            warehouseService.DepositAsync(clientSession,
                packet.PartnerBackpack ? WarehouseType.PetWarehouse : WarehouseType.Warehouse,
                packet.Inventory, packet.Slot, packet.Amount, packet.NewSlot);
    }
}
