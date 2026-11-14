//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Instances.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public interface IItemThrowService
    {
        // Lays each pile on a free cell around the spot, for anyone to pick up.
        Task ThrowAsync(MapInstance mapInstance, Spot around, IEnumerable<ItemPile> piles);
    }
}
