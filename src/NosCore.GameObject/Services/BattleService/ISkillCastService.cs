//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.BattleService;

// The cooldown and mana a character's cast costs. Checked before the battle service runs and
// spent only once it accepts the cast, so a refused cast can be retried straight away.
public interface ISkillCastService
{
    // The base skill of this cast id, or null when the character has not learned it.
    CharacterSkill? FindLearned(PlayerComponentBundle character, long castId);

    SkillCastRefusal Check(PlayerComponentBundle character, CharacterSkill skill, out int mpCost);

    // Starts the cooldown and takes the mana, showing the caster the bar it leaves.
    Task SpendAsync(PlayerComponentBundle character, CharacterSkill skill, int mpCost);

    // A skill's cooldown once the caster's buffs and gear have lengthened or shortened it.
    Duration Cooldown(PlayerComponentBundle character, short cooldown);
}
