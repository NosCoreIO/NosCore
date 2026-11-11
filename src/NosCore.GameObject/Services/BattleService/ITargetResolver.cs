//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Services.BattleService;

// Expands a single cast into the concrete list of entities to hit. Single-target skills
// return just the primary; AOE skills additionally include anyone within TargetRange who
// qualifies as an enemy of the caster. The primary is always first in the list so the
// queue and packet serialization can treat it specially (e.g. big impact animation).
public interface ITargetResolver
{
    IReadOnlyList<IAliveEntity> Resolve(IAliveEntity attacker, IAliveEntity primaryTarget, SkillInfo skill);

    // Everyone the attacker may hit within the skill's target range of a map cell.
    IReadOnlyList<IAliveEntity> ResolveArea(IAliveEntity attacker, MapInstance mapInstance, short x, short y,
        SkillInfo skill);
}
