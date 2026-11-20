//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Ecs.Components;

// The cards of the charge skill the player last cast, held until the next attack spends them.
public record struct ChargeComponent(IReadOnlyList<BCardDto>? ChargeBCards);
