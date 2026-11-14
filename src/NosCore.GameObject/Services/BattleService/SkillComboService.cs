//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.BattleService;

// A skill with combo rows (most basic attacks) changes its animation on given uses of a streak:
// using any other skill breaks the streak, and reaching the last combo row starts it over.
public sealed class SkillComboService(List<ComboDto> combos) : ISkillComboService, ISingletonService
{
    private readonly IReadOnlyDictionary<short, ComboDto[]> _bySkill = combos
        .GroupBy(c => c.SkillVNum)
        .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Hit).ToArray());

    public ComboDto? Advance(PlayerComponentBundle player, short skillVnum)
    {
        var hit = (short)(player.ComboSkillVnum == skillVnum ? player.ComboHit + 1 : 1);
        ComboDto? combo = null;
        if (_bySkill.TryGetValue(skillVnum, out var rows))
        {
            combo = rows.FirstOrDefault(c => c.Hit == hit);
            if (combo != null && combo.Hit == rows[^1].Hit)
            {
                hit = 0;
            }
        }

        player.ComboSkillVnum = skillVnum;
        player.ComboHit = hit;
        return combo;
    }
}
