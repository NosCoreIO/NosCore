//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public interface IMapItemGenerationService
    {
        MapItem Create(MapInstance mapInstance, IItemInstance itemInstance, short positionX, short positionY);
    }
}
