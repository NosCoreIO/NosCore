//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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