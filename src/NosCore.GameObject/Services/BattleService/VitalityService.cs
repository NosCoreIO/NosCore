//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.GameObject.Ecs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.Networking;

namespace NosCore.GameObject.Services.BattleService;

public sealed class VitalityService(
    IHpService hpService,
    IMpService mpService,
    EquipmentService.IEquipmentStatsService equipmentStatsService,
    IBuffService buffService,
    SkillService.IPassiveSkillService passiveSkillService) : IVitalityService, ISingletonService
{
    public bool Refresh(PlayerComponentBundle character)
    {
        var gear = equipmentStatsService.Resolve(character);
        var cards = gear.BCards.Concat(buffService.GetActiveBuffs(character).SelectMany(b => b.BCards))
            .Concat(passiveSkillService.GetBCards(character));

        var baseHp = (int)hpService.GetHp(character.Class, character.Level);
        var baseMp = (int)mpService.GetMp(character.Class, character.Level);
        var hp = baseHp + gear.Hp;
        var mp = baseMp + gear.Mp;

        int hpPercent = gear.SpecialistHpMpPercent, mpPercent = gear.SpecialistHpMpPercent;

        int additionalHpPercent = 0, additionalHpCap = 0;
        int additionalMpPercent = 0, additionalMpCap = 0;

        foreach (var card in cards)
        {
            var type = (BCardType.CardType)card.Type;
            if (type != BCardType.CardType.Quest && type != BCardType.CardType.MaxHpmp && type != BCardType.CardType.Recovery)
            {
                continue;
            }

            var value = BattleStatsProvider.ScaleByLevel(card, character.Level);
            switch (card.Effect())
            {
                case BCardEffect.QuestAdditionalHpPercent:
                    additionalHpPercent += value;
                    additionalHpCap = Math.Max(additionalHpCap, card.SecondData);
                    break;
                case BCardEffect.QuestAdditionalMpPercent:
                    additionalMpPercent += value;
                    additionalMpCap = Math.Max(additionalMpCap, card.SecondData);
                    break;

                case BCardEffect.MaxHpmpMaximumHpIncreased: hp += value; break;
                case BCardEffect.MaxHpmpMaximumHpDecreased: hp -= value; break;
                case BCardEffect.MaxHpmpMaximumMpIncreased: mp += value; break;
                case BCardEffect.MaxHpmpMaximumMpDecreased: mp -= value; break;
                case BCardEffect.MaxHpmpIncreasesMaximumHp: hpPercent += value; break;
                case BCardEffect.MaxHpmpDecreasesMaximumHp: hpPercent -= value; break;
                case BCardEffect.MaxHpmpIncreasesMaximumMp: mpPercent += value; break;
                case BCardEffect.MaxHpmpDecreasesMaximumMp: mpPercent -= value; break;
                case BCardEffect.RecoveryMaxHpIncreased: hpPercent += value; break;
                case BCardEffect.RecoveryMaxHpDecreased: hpPercent -= value; break;
                case BCardEffect.RecoveryMaxMpIncreased: mpPercent += value; break;
                case BCardEffect.RecoveryMaxMpDecreased: mpPercent -= value; break;

                // Subtype 51 moves both maxima together.
                case BCardEffect.MaxHpmpMaximumHpmpIncreased: hp += value; mp += value; break;
                case BCardEffect.MaxHpmpMaximumHpmpDecreased: hp -= value; mp -= value; break;
            }
        }

        hp += hp * hpPercent / 100;
        mp += mp * mpPercent / 100;

        hp += BoostedAddition(hp - baseHp, hp, additionalHpPercent, additionalHpCap);
        mp += BoostedAddition(mp - baseMp, mp, additionalMpPercent, additionalMpCap);

        hp = Math.Max(1, hp);
        mp = Math.Max(1, mp);

        if (character.MaxHp == hp && character.MaxMp == mp)
        {
            return false;
        }

        character.MaxHp = hp;
        character.MaxMp = mp;

        character.Hp = Math.Min(character.Hp, hp);
        character.Mp = Math.Min(character.Mp, mp);
        return true;
    }

    public async Task RefreshAndNotifyAsync(PlayerComponentBundle character)
    {
        if (!Refresh(character))
        {
            return;
        }

        if (character is Ecs.PlayerComponentBundle player)
        {
            await player.SendPacketAsync(player.GenerateStat()).ConfigureAwait(false);
        }
    }

    public static int BoostedAddition(int additional, int maximum, int percent, int capPercent)
    {
        if (percent <= 0 || additional <= 0)
        {
            return 0;
        }

        var boost = additional * percent / 100;
        if (capPercent <= 0)
        {
            return boost;
        }

        // Already at or over the ceiling: the effect adds nothing rather than taking away.
        var ceiling = maximum * capPercent / 100;
        return Math.Min(boost, Math.Max(0, ceiling - additional));
    }
}
