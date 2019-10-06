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
    public enum FamilyLogType : byte
    {
        DailyMessage = 1,
        RaidWon = 2,
        RainbowBattle = 3,
        FamilyXp = 4,
        FamilyLevelUp = 5,
        LevelUp = 6,
        ItemUpgraded = 7,
        RightChanged = 8,
        AuthorityChanged = 9,
        FamilyManaged = 10,
        UserManaged = 11,
        WareHouseAdded = 12,
        WareHouseRemoved = 13
    }
}