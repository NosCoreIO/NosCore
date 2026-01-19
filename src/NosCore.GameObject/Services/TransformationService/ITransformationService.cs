//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TransformationService
{
    public interface ITransformationService
    {
        Task RemoveSpAsync(Character character);

        Task ChangeSpAsync(Character character);

        Task ChangeVehicleAsync(Character character, Item item);

        Task RemoveVehicleAsync(Character character);
    }
}
