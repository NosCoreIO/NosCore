//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.GameObject.Services.ProductionService;

namespace NosCore.GameObject.Ecs.Components;

// What an NPC can produce in its production window.
public record struct RecipesComponent(IReadOnlyList<Recipe>? Recipes);
