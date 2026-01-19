//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Data.Enumerations.Interaction
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum TalentArenaOptionType : byte
    {
        Watch = 0,
        Nothing = 1,
        Call = 2,
        WatchAndCall = 3
    }
}
