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

namespace NosCore.Data.Enumerations.Quest
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum QuestRewardType : byte
    {
        Gold = 1,
        BaseGoldByAmount = 2, // Base Gold * amount
        Exp = 3,
        PercentExp = 4, // Percent xp of the player (ex: give 10%)
        JobExp = 5,
        EtcMainItem = 7,
        WearItem = 8,
        Reput = 9,
        CapturedGold = 10, // Give the number of capturated monsters * amount in Gold
        UnknowGold = 11,
        PercentJobExp = 12,
        Unknow = 13 //never used but it is in the dat file
    }
}