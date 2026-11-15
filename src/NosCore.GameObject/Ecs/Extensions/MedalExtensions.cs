//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ItemGenerationService;

namespace NosCore.GameObject.Ecs.Extensions;

// A medal is a timed static bonus whose experience, gold and fame bonuses are the BCards of its item.
public static class MedalExtensions
{
    private static readonly Dictionary<ItemEffectType, StaticBonusType> Bonuses = new()
    {
        [ItemEffectType.MedalOfErenia] = StaticBonusType.MedalOfErenia,
        [ItemEffectType.AdventurersMedal] = StaticBonusType.AdventurersMedal,
        [ItemEffectType.MedalOfFriendship] = StaticBonusType.MedalOfFriendship,
        [ItemEffectType.MimisMedal] = StaticBonusType.MimisMedal,
    };

    public static StaticBonusType? MedalBonus(this ItemEffectType effect) =>
        Bonuses.TryGetValue(effect, out var bonus) ? bonus : null;

    public static IEnumerable<BCardDto> MedalBCards(this PlayerComponentBundle player, IItemCatalog itemCatalog, ICardCatalog cardCatalog) =>
        Bonuses.Where(b => player.StaticBonusList.Any(s => s.StaticBonusType == b.Value))
            .SelectMany(b => itemCatalog.GetItemWithEffect(b.Key) is { } medal ? cardCatalog.GetItemBCards(medal.VNum) : []);
}
