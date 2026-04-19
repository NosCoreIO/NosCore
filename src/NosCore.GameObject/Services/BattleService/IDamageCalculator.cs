//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService;

// Pure function: given resolved stats for both sides and the skill being used, compute
// the final damage + how to present the hit to the client (miss/crit/normal). Kept free
// of ECS/networking dependencies so tests can pin formulas with plain struct inputs.
public interface IDamageCalculator
{
    DamageResult Calculate(CombatStats attacker, CombatStats defender, SkillInfo skill);
}
