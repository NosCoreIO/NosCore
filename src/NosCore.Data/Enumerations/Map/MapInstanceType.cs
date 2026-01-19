//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Data.Enumerations.Map
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum MapInstanceType
    {
        BaseMapInstance,
        NormalInstance,
        LodInstance,
        TimeSpaceInstance,
        RaidInstance,
        FamilyRaidInstance,
        TalentArenaMapInstance,
        Act4Instance,
        IceBreakerInstance,
        RainbowBattleInstance,
        ArenaInstance
    }
}
