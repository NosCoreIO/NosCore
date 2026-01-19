//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Data.Enumerations.Interaction
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum ScriptedInstanceType : byte
    {
        TimeSpace = 0,
        Raid = 1,
        RaidAct4 = 2
    }
}
