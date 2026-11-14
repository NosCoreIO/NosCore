//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.Algorithm.SpecialistPointService;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Services.SpecialistCardService;

public sealed class SpecialistCardService(ISpecialistPointService specialistPointService)
    : ISpecialistCardService, ISingletonService
{
    public int GetFreePoints(SpecialistInstance specialist) =>
        specialistPointService.GetSpecialistPoints(specialist.SpLevel, specialist.Upgrade)
        - specialist.SlDamage - specialist.SlDefence - specialist.SlElement - specialist.SlHp;

    public bool TrySpendPoints(SpecialistInstance specialist, short attack, short defence, short element, short hpMp)
    {
        if (attack < 0 || defence < 0 || element < 0 || hpMp < 0
            || attack + defence + element + hpMp > GetFreePoints(specialist))
        {
            return false;
        }

        var maximumLevel = specialistPointService.GetMaximumStatLevel(specialist.Upgrade);
        if (specialistPointService.GetAttackLevel((short)(specialist.SlDamage + attack)) > maximumLevel
            || specialistPointService.GetDefenceLevel((short)(specialist.SlDefence + defence)) > maximumLevel
            || specialistPointService.GetElementLevel((short)(specialist.SlElement + element)) > maximumLevel
            || specialistPointService.GetHpMpLevel((short)(specialist.SlHp + hpMp)) > maximumLevel)
        {
            return false;
        }

        specialist.SlDamage += attack;
        specialist.SlDefence += defence;
        specialist.SlElement += element;
        specialist.SlHp += hpMp;
        return true;
    }

    public bool TryResetPoints(SpecialistInstance specialist)
    {
        if (specialist.SlDamage == 0 && specialist.SlDefence == 0 && specialist.SlElement == 0 && specialist.SlHp == 0)
        {
            return false;
        }

        specialist.SlDamage = 0;
        specialist.SlDefence = 0;
        specialist.SlElement = 0;
        specialist.SlHp = 0;
        return true;
    }

    public SpecialistPointBonus GetBonus(SpecialistInstance specialist, SpecialistLevelBoost boost = default)
    {
        var maximum = specialistPointService.GetMaximumStatLevel(specialist.Upgrade);
        int Level(int level, int extra) => extra == 0 ? level : Math.Min(level + extra, Math.Max(level, maximum));
        return specialistPointService.GetAttackBonus(Level(specialistPointService.GetAttackLevel(specialist.SlDamage), boost.Attack))
            + specialistPointService.GetDefenceBonus(Level(specialistPointService.GetDefenceLevel(specialist.SlDefence), boost.Defence))
            + specialistPointService.GetElementBonus(Level(specialistPointService.GetElementLevel(specialist.SlElement), boost.Element))
            + specialistPointService.GetHpMpBonus(Level(specialistPointService.GetHpMpLevel(specialist.SlHp), boost.HpMp));
    }
}
