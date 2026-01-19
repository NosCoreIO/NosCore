//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Interaction
{
    public enum ReceiverType : byte
    {
        All = 1,
        AllExceptMe = 2,
        AllInRange = 3,
        OnlySomeone = 4,
        AllNoEmoBlocked = 5,
        AllNoHeroBlocked = 6,
        Group = 7,
        AllExceptGroup = 8,
        AllExceptMeAndBlacklisted = 9
    }
}
