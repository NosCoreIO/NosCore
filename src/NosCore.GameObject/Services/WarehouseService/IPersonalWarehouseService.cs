//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.WarehouseService
{
    public interface IPersonalWarehouseService
    {
        Task SendCarriedStoragesAsync(ClientSession session);

        Task OpenAsync(ClientSession session, WarehouseType warehouseType);

        Task DepositAsync(ClientSession session, WarehouseType warehouseType, PocketType pocket, short slot, short amount,
            short warehouseSlot);

        Task WithdrawAsync(ClientSession session, WarehouseType warehouseType, short warehouseSlot, short amount);

        Task MoveAsync(ClientSession session, WarehouseType warehouseType, short sourceSlot, short amount, short destinationSlot);
    }
}
