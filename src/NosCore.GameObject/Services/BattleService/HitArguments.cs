// __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.GameObject.Ecs.Interfaces;

using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.BattleService;

public record HitArguments()
{
    public long SkillId { get; set; }

    // A skill the caller has already checked the caster may use, cast instead of the one SkillId resolves to.
    public SkillDto? Skill { get; set; }

    public IReadOnlyList<IAliveEntity>? ListedTargets { get; set; }
}
