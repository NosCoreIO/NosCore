//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Data.Enumerations.Family
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum FamilyAuthority : byte
    {
        Head = 0,
        Assistant = 1,
        Manager = 2,
        Member = 3
    }
}
