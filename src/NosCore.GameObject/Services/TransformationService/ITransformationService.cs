//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TransformationService
{
    public interface ITransformationService
    {
        Task RemoveSpAsync(ClientSession session);

        Task ChangeSpAsync(ClientSession session);

        Task ChangeVehicleAsync(ClientSession session, Item item);

        Task RemoveVehicleAsync(ClientSession session);
    }
}
