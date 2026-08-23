//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Services.FamilyService;
using System.Collections.Concurrent;

namespace NosCore.GameObject.Ecs.Components;

/// <summary>
/// The people a character is connected to: who has asked them into a group, and which family
/// they belong to.
/// </summary>
/// <remarks>
/// The family sits here rather than in a component of its own for two reasons. It is social
/// state, which is what this component is for; and Arch's World.Create runs out of generic
/// overloads at the number of components the player bundle already has, so a twenty-sixth would
/// have to be added separately and would be easy to forget on the map-change path.
/// </remarks>
public record struct PlayerSocialComponent(
    ConcurrentDictionary<long, long> GroupRequestCharacterIds,
    Instant? LastGroupRequest,
    FamilyCharacter? FamilyCharacter);
