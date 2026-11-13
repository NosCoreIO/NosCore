//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TransformationService
{
    public interface ITransformationService
    {
        Task RemoveSpAsync(ClientSession session);

        Task ChangeSpAsync(ClientSession session);

        // vehicle sets the speed and the Speed Booster; look sets the sprite.
        Task ChangeVehicleAsync(ClientSession session, ItemDto vehicle, ItemDto look);

        Task RemoveVehicleAsync(ClientSession session);
    }
}
