//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.SkillService;

public sealed class SkillCatalog(List<SkillDto> skills) : ISkillCatalog, ISingletonService
{
    private readonly FrozenDictionary<int, SkillDto> _skills =
        skills.GroupBy(s => (int)s.SkillVNum).ToFrozenDictionary(g => g.Key, g => g.First());

    public SkillDto? GetSkill(int skillVNum) => _skills.GetValueOrDefault(skillVNum);
}
