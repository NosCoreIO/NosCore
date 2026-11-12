//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public interface IFamilyWarehouseService
    {
        Task OpenAsync(ClientSession session);

        Task DepositAsync(ClientSession session, PocketType pocket, short slot, short amount, short warehouseSlot);

        Task WithdrawAsync(ClientSession session, short warehouseSlot, short amount);

        Task MoveAsync(ClientSession session, short sourceSlot, short amount, short destinationSlot);
    }
}
