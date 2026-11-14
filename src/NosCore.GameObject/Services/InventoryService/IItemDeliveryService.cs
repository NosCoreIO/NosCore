//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Services.InventoryService
{
    public interface IItemDeliveryService
    {
        // Puts the item in the player's inventory, or in their parcel box when it does not fit.
        // Returns false when it went to the parcel box.
        Task<bool> DeliverAsync(PlayerComponentBundle player, IItemInstance item);
    }
}
