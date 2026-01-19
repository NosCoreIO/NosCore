//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Data.Enumerations.Character
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum FactionType : byte
    {
        Neutral = 0,
        Angel = 1,
        Demon = 2
    }
}
