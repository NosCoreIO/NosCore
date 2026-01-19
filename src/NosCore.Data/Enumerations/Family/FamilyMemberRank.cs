//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Data.Enumerations.Family
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum FamilyMemberRank : byte
    {
        Nothing = 0,
        OldUncle = 1,
        OldAunt = 2,
        Father = 3,
        Mother = 4,
        Uncle = 5,
        Aunt = 6,
        Brother = 7,
        Sister = 8,
        Spouse = 9,
        Brother2 = 10,
        Sister2 = 11,
        OldSon = 12,
        OldDaugter = 13,
        MiddleSon = 14,
        MiddleDaughter = 15,
        YoungSon = 16,
        YoungDaugter = 17,
        OldLittleSon = 18,
        OldLittleDaughter = 19,
        LittleSon = 20,
        LittleDaughter = 21,
        MiddleLittleSon = 22,
        MiddleLittleDaugter = 23
    }
}
