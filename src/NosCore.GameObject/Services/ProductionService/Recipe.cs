//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.ProductionService
{
    public class Recipe : RecipeDto
    {
        public List<RecipeItemDto> Items { get; set; } = new();
    }
}
