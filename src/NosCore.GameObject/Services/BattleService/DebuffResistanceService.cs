//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Frozen;
using System.Collections.Generic;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.SkillService;

namespace NosCore.GameObject.Services.BattleService;

public sealed class DebuffResistanceService(
    IEquipmentStatsService equipmentStatsService,
    IPassiveSkillService passiveSkillService,
    ICardCatalog cardCatalog) : IDebuffResistanceService, ISingletonService
{
    // The data names a buff family and a level but no percentage; the item descriptions only
    // promise "a high chance of protection".
    private const int FamilyProtection = 80;

    private static readonly FrozenDictionary<BCardEffect, (CardKind? Kind, bool Increases)> Resistances =
        new Dictionary<BCardEffect, (CardKind?, bool)>
        {
            [BCardEffect.DebuffResistanceIncreaseBadEffectChance] = (null, true),
            [BCardEffect.DebuffResistanceNeverBadEffectChance] = (null, false),
            [BCardEffect.DebuffResistanceIncreaseBadGeneralEffectChance] = (CardKind.General, true),
            [BCardEffect.DebuffResistanceNeverBadGeneralEffectChance] = (CardKind.General, false),
            [BCardEffect.DebuffResistanceIncreaseBadMagicEffectChance] = (CardKind.Magic, true),
            [BCardEffect.DebuffResistanceNeverBadMagicEffectChance] = (CardKind.Magic, false),
            [BCardEffect.DebuffResistanceIncreaseBadToxicEffectChance] = (CardKind.Toxic, true),
            [BCardEffect.DebuffResistanceNeverBadToxicEffectChance] = (CardKind.Toxic, false),
            [BCardEffect.DebuffResistanceIncreaseBadDiseaseEffectChance] = (CardKind.Disease, true),
            [BCardEffect.DebuffResistanceNeverBadDiseaseEffectChance] = (CardKind.Disease, false),
        }.ToFrozenDictionary();

    public int ChanceModifier(IAliveEntity target, CardDto card, IReadOnlyCollection<BuffInstance> activeBuffs)
    {
        if (card.BuffType != BuffType.Bad)
        {
            return 0;
        }

        var modifier = 0;
        foreach (var buff in activeBuffs)
        {
            modifier += Sum(buff.BCards, card);
        }

        if (target is PlayerComponentBundle character)
        {
            modifier += Sum(equipmentStatsService.Resolve(character).BCards, card);
            modifier += Sum(passiveSkillService.GetBCards(character), card);
        }

        return modifier;
    }

    private int Sum(IReadOnlyList<BCardDto> bCards, CardDto card)
    {
        var sum = 0;
        for (var i = 0; i < bCards.Count; i++)
        {
            sum += Modifier(bCards[i], card);
        }

        return sum;
    }

    private int Modifier(BCardDto bCard, CardDto card)
    {
        var effect = bCard.Effect();
        if (effect == BCardEffect.BuffPreventingBadEffect)
        {
            return bCard.FirstData == card.GroupId && card.Level <= bCard.SecondData ? -FamilyProtection : 0;
        }

        if (effect == BCardEffect.SpecialisationBuffResistanceResistanceToEffect)
        {
            var named = cardCatalog.GetCard((short)bCard.SecondData);
            return named != null && named.GroupId == card.GroupId && card.Level <= named.Level ? -bCard.FirstData : 0;
        }

        if (!Resistances.TryGetValue(effect, out var resistance) || card.Level > bCard.FirstData
            || (resistance.Kind != null && resistance.Kind != card.Kind))
        {
            return 0;
        }

        return resistance.Increases ? bCard.SecondData : -bCard.SecondData;
    }
}
