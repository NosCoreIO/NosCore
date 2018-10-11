using System.Diagnostics.CodeAnalysis;

namespace NosCore.Shared.Enumerations.Map
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