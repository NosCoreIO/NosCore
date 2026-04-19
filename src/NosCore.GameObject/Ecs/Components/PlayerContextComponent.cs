//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ShopService;

namespace NosCore.GameObject.Ecs.Components;

public record struct PlayerContextComponent(
    MapInstance MapInstance,
    Group? Group,
    Shop? Shop);
