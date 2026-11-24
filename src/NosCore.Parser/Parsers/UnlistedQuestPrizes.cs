//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;
using System.Collections.Generic;

namespace NosCore.Parser.Parsers
{
    // Prizes qstprize.dat does not list. The closing dialog of the Pyjama Fairy's quest line hands over the
    // Pyjama Specialist Card, and claiming the line's last quest pays it out.
    public static class UnlistedQuestPrizes
    {
        public static readonly IReadOnlyList<UnlistedQuestPrize> All =
        [
            new(2007, QuestRewardType.WearItem, 900),
        ];
    }

    public sealed record UnlistedQuestPrize(short QuestId, QuestRewardType RewardType, short ItemVNum)
    {
        // Ids below zero keep clear of qstprize.dat's own.
        public short QuestRewardId => (short)-QuestId;
    }
}
