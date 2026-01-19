//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Data.Enumerations.Interaction
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum RespawnType : byte
    {
        DefaultAct1 = 0,
        ReturnAct1 = 1,
        DefaultAct3 = 2,
        DefaultAct5 = 3,
        ReturnAct5 = 4,
        DefaultAct6 = 5,
        DefaultAct62 = 6,
        DefaultOasis = 7
    }
}
