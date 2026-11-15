//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Collections.Generic;
using NosCore.Data.Enumerations.Buff;

namespace NosCore.Parser.Parsers
{
    // What each medal gives, as its English description lists it. Combat experience also raises job,
    // pet and partner experience on the server's side, so one figure covers the description's four.
    // Left out: the Medal of Friendship's "+5% to party members' points", which names no points the
    // server keeps.
    public static class Medals
    {
        public static readonly IReadOnlyDictionary<short, Medal> All = new Dictionary<short, Medal>
        {
            [1115] = new(Days: 30, Experience: 15, Gold: 20, Reputation: 20),
            [1116] = new(Days: 30, Experience: 15),
            [1149] = new(Days: 10, Experience: 5),
            [1187] = new(Days: 3, Experience: 30, Gold: 20, Reputation: 20),
        };

        public static IEnumerable<(BCardEffect Effect, int Value)> Bonuses(Medal medal)
        {
            if (medal.Experience != 0)
            {
                yield return (BCardEffect.ItemExpIncreased, medal.Experience);
            }

            if (medal.Gold != 0)
            {
                yield return (BCardEffect.ItemIncreaseEarnedGold, medal.Gold);
            }

            if (medal.Reputation != 0)
            {
                yield return (BCardEffect.IncreaseFameReceived, medal.Reputation);
            }
        }
    }

    public sealed record Medal(int Days, int Experience, int Gold = 0, int Reputation = 0);
}
