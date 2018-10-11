using System.Diagnostics.CodeAnalysis;

namespace NosCore.Shared.Enumerations.Map
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum PortalType : sbyte
    {
        MapPortal = -1,
        TsNormal = 0, // same over >127 - sbyte
        Closed = 1,
        Open = 2,
        Miniland = 3,
        TsEnd = 4,
        TsEndClosed = 5,
        Exit = 6,
        ExitClosed = 7,
        Raid = 8,
        Effect = 9, // same as 13 - 19 and 20 - 126
        BlueRaid = 10,
        DarkRaid = 11,
        TimeSpace = 12,
        ShopTeleport = 20
    }
}