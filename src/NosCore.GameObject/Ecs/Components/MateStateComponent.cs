//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.MateService;

namespace NosCore.GameObject.Ecs.Components;

/// <summary>
/// What makes an entity somebody's mate rather than a monster: the stored row it came from and
/// the character it belongs to.
/// </summary>
public record struct MateStateComponent(Mate Mate, long OwnerId);
