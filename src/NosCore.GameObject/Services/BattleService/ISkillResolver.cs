//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService;

// Picks the right SkillDto for a given caster + cast intent. For characters this indexes
// into their learned skill dictionary; for monsters/pets it picks from NpcMonsterSkill
// (or falls back to BasicSkill). Null return = the caster cannot use this skill now.
public interface ISkillResolver
{
    SkillInfo? Resolve(IAliveEntity caster, long castId);
}
