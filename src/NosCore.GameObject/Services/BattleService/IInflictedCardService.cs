//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

/// <summary>
/// The cards a skill inflicts on what it hits: the poison of a poisoned arrow, the stun of Star
/// Attack, the rage of Hit of Rage.
///
/// A skill does not carry the effect - it carries a BCard of type 25 saying "with N% chance,
/// apply Card number M". Card M is a real entry of <c>Card.dat</c>, with its own duration and its
/// own BCards. Without the step back from M to the Card, that reference goes nowhere, and it is
/// the most widespread effect in the game: 1344 of the skills declare one.
/// </summary>
public interface IInflictedCardService
{
    /// <summary>
    /// Rolls each type 25 BCard the skill declares and applies, or removes, the card it names.
    /// </summary>
    Task InflictAsync(IAliveEntity target, IAliveEntity? caster, IReadOnlyList<BCardDto> skillBCards);
}
