//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

// The cooldown and mana a character's cast costs. Checked before the battle service runs and
// spent only once it accepts the cast, so a refused cast can be retried straight away.
public interface ISkillCastService
{
    // The base skill of this cast id, or null when the character has not learned it.
    CharacterSkill? FindLearned(ICharacterEntity character, long castId);

    SkillCastRefusal Check(ICharacterEntity character, CharacterSkill skill, out int mpCost);

    void Spend(ICharacterEntity character, CharacterSkill skill, int mpCost);
}
