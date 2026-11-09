//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;

namespace NosCore.Tests.Shared
{
    // Card 169, "Invincible in PvP", as the parser stores it from the client's card data.
    public static class RevivalProtectionCard
    {
        public const short CardId = 169;

        public static CardDto Card => new() { CardId = CardId, Duration = 50 };

        public static IReadOnlyList<BCardDto> BCards =>
        [
            Make(BCardEffect.NoDefeatAndNoDamageNeverCauseDamage, 1, 0),
            Make(BCardEffect.NoDefeatAndNoDamageNeverReceiveDamage, 0, 0),
            Make(BCardEffect.DebuffResistanceNeverBadEffectChance, 99, 100),
        ];

        private static BCardDto Make(BCardEffect effect, int firstData, int secondData) => new()
        {
            CardId = CardId,
            Type = effect.Type(),
            SubType = effect.SubType(),
            FirstData = firstData,
            SecondData = secondData,
        };
    }
}
