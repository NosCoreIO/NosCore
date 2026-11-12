//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.MateExperienceService;
using NosCore.Algorithm.MateHpService;
using NosCore.Algorithm.MateMpService;
using NosCore.Algorithm.MateStatService;
using NosCore.Algorithm.MonsterStatService;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.MateService;

public sealed class MateStatsService(IMateExperienceService mateExperienceService, IMateHpService mateHpService,
    IMateMpService mateMpService, IMateStatService mateStatService) : IMateStatsService, ISingletonService
{
    public void ApplyLevel(Mate mate)
    {
        var monster = mate.NpcMonster;
        mate.XpLoad = mate.MateType == MateType.Pet
            ? mateExperienceService.GetPetExperience(mate.Level)
            : mateExperienceService.GetPartnerExperience(mate.Level);
        mate.MaxHp = (int)mateHpService.GetHp(monster.Race, mate.Level, (short)monster.PetInfoVal1) + monster.HpBonus;
        mate.MaxMp = (int)mateMpService.GetMp(monster.Race, mate.Level, (short)monster.PetInfoVal3) + monster.MpBonus;

        // Partners grow through their gear, which these curves do not cover, so they keep their monster's stats.
        if (mate.MateType != MateType.Pet)
        {
            mate.DamageMinimum = monster.DamageMinimum;
            mate.DamageMaximum = monster.DamageMaximum;
            mate.Concentrate = monster.Concentrate;
            mate.CloseDefence = monster.CloseDefence;
            mate.DistanceDefence = monster.DistanceDefence;
            mate.MagicDefence = monster.MagicDefence;
            mate.Dodge = monster.DefenceDodge;
            return;
        }

        var growth = new MateGrowth((short)monster.PetInfoVal1, (short)monster.PetInfoVal2, (short)monster.PetInfoVal3);
        var attackClass = (MonsterAttackClass)monster.AttackClass;
        mate.DamageMinimum = (short)(mateStatService.GetMinimumDamage(monster.Race, mate.Level, attackClass, growth) + monster.DamageMinimumBonus);
        mate.DamageMaximum = (short)(mateStatService.GetMaximumDamage(monster.Race, mate.Level, attackClass, growth) + monster.DamageMaximumBonus);
        mate.Concentrate = (short)(mateStatService.GetHitRate(monster.Race, mate.Level, attackClass, growth) + monster.ConcentrateBonus);
        mate.CloseDefence = (short)(mateStatService.GetCloseDefence(monster.Race, mate.Level, growth) + monster.CloseDefenceBonus);
        mate.DistanceDefence = (short)(mateStatService.GetDistanceDefence(monster.Race, mate.Level, growth) + monster.DistanceDefenceBonus);
        mate.MagicDefence = (short)(mateStatService.GetMagicDefence(monster.Race, mate.Level, growth) + monster.MagicDefenceBonus);
        mate.Dodge = (short)(mateStatService.GetDodge(monster.Race, mate.Level, growth) + monster.DodgeBonus);
    }
}
