//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService.Model;

// A buff/debuff currently attached to an entity. CardDto is the design-time description;
// BCards are the resolved stat modifiers applied while the buff is active.
public sealed record BuffInstance(
    short CardId,
    BuffType BuffType,
    IAliveEntity? Caster,
    Instant StartedAt,
    Instant ExpiresAt,
    IReadOnlyList<BCardDto> BCards);
