//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Data.Enumerations.Quest
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum QuestRewardType : byte
    {
        Gold = 1,
        BaseGoldByAmount = 2, // Base Gold * amount
        Exp = 3,
        PercentExp = 4, // Percent xp of the player (ex: give 10%)
        JobExp = 5,
        EtcMainItem = 7,
        WearItem = 8,
        Reput = 9,
        CapturedGold = 10, // Give the number of capturated monsters * amount in Gold
        UnknowGold = 11,
        PercentJobExp = 12,
        Unknow = 13 //never used but it is in the dat file
    }
}
