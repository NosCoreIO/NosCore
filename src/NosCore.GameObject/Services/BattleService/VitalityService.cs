//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
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
    IBuffService buffService) : IVitalityService, ISingletonService
{
    public bool Refresh(ICharacterEntity character)
    {
        if (character is not Ecs.PlayerComponentBundle player)
        {
            return false;
        }

        var gear = equipmentStatsService.Resolve(character);
        var cards = gear.BCards.Concat(buffService.GetActiveBuffs(character).SelectMany(b => b.BCards));

        var baseHp = (int)hpService.GetHp(character.Class, character.Level);
        var baseMp = (int)mpService.GetMp(character.Class, character.Level);
        var hp = baseHp + gear.Hp;
        var mp = baseMp + gear.Mp;

        int hpPercent = 0, mpPercent = 0;

        int additionalHpPercent = 0, additionalHpCap = 0;
        int additionalMpPercent = 0, additionalMpCap = 0;

        foreach (var card in cards)
        {
            if ((BCardType.CardType)card.Type == BCardType.CardType.Quest)
            {
                var first = BattleStatsProvider.ScaleByLevel(card, character.Level);
                switch ((AdditionalTypes.Quest)card.SubType)
                {
                    case AdditionalTypes.Quest.AdditionalHpPercent:
                        additionalHpPercent += first;
                        additionalHpCap = Math.Max(additionalHpCap, card.SecondData);
                        break;
                    case AdditionalTypes.Quest.AdditionalMpPercent:
                        additionalMpPercent += first;
                        additionalMpCap = Math.Max(additionalMpCap, card.SecondData);
                        break;
                }

                continue;
            }

            if ((BCardType.CardType)card.Type != BCardType.CardType.MaxHpmp)
            {
                continue;
            }

            var value = BattleStatsProvider.ScaleByLevel(card, character.Level);
            switch ((AdditionalTypes.MaxHpmp)card.SubType)
            {
                case AdditionalTypes.MaxHpmp.MaximumHpIncreased: hp += value; break;
                case AdditionalTypes.MaxHpmp.MaximumHpDecreased: hp -= value; break;
                case AdditionalTypes.MaxHpmp.MaximumMpIncreased: mp += value; break;
                case AdditionalTypes.MaxHpmp.MaximumMpDecreased: mp -= value; break;
                case AdditionalTypes.MaxHpmp.IncreasesMaximumHp: hpPercent += value; break;
                case AdditionalTypes.MaxHpmp.DecreasesMaximumHp: hpPercent -= value; break;
                case AdditionalTypes.MaxHpmp.IncreasesMaximumMp: mpPercent += value; break;
                case AdditionalTypes.MaxHpmp.DecreasesMaximumMp: mpPercent -= value; break;

                // Subtype 51 moves both maxima together.
                case AdditionalTypes.MaxHpmp.MaximumHpmpIncreased: hp += value; mp += value; break;
                case AdditionalTypes.MaxHpmp.MaximumHpmpDecreased: hp -= value; mp -= value; break;
            }
        }

        hp += hp * hpPercent / 100;
        mp += mp * mpPercent / 100;

        hp += BoostedAddition(hp - baseHp, hp, additionalHpPercent, additionalHpCap);
        mp += BoostedAddition(mp - baseMp, mp, additionalMpPercent, additionalMpCap);

        hp = Math.Max(1, hp);
        mp = Math.Max(1, mp);

        if (player.MaxHp == hp && player.MaxMp == mp)
        {
            return false;
        }

        player.MaxHp = hp;
        player.MaxMp = mp;

        player.Hp = Math.Min(player.Hp, hp);
        player.Mp = Math.Min(player.Mp, mp);
        return true;
    }

    public async Task RefreshAndNotifyAsync(ICharacterEntity character)
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
