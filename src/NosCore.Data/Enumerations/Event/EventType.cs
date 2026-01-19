//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Data.Enumerations.Event
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum EventType
    {
        Instantbattle,
        Lod,
        MinilandRefresh,
        LodDh,
        RankingreFresh,
        TalentArena,
        MasterArena,
        IceBreaker,
        Act4Ship
    }
}
