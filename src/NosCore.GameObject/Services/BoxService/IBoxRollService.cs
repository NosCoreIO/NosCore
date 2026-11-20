//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.Dto;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Services.BoxService
{
    public interface IBoxRollService
    {
        // One of the sets the box holds, picked by weight; a class box prefers the sets its opener can use.
        // Null when the box holds nothing.
        IReadOnlyList<RollGeneratedItemDto>? Roll(IItemInstance box, PlayerComponentBundle opener);
    }
}
