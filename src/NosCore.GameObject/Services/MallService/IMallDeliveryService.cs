//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.MallService
{
    public interface IMallDeliveryService
    {
        // Records an item bought or collected in the NosMall; it reaches the character on their next mall sync.
        Task<bool> EnqueueAsync(long characterId, short vNum, short amount);

        // Sends every item still waiting for the player to their parcel box.
        Task DeliverPendingAsync(PlayerComponentBundle player);
    }
}
