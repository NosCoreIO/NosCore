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
    public enum QuestType : short
    {
        Hunt = 1,
        SpecialCollect = 2,
        CollectInRaid = 3,
        Brings = 4,
        CaptureWithoutGettingTheMonster = 5,
        Capture = 6,
        TimesSpace = 7,
        Product = 8,
        NumberOfKill = 9,
        TargetReput = 10,
        TsPoint = 11,
        Dialog1 = 12,
        CollectInTs = 13, //Collect in TimeSpace
        Required = 14,
        Wear = 15,
        Needed = 16,
        Collect = 17, // Collect basic items & quests items
        TransmitGold = 18,
        GoTo = 19,
        CollectMapEntity = 20, // Collect from map entity ( Ice Flower / Water )
        Use = 21,
        Dialog2 = 22,
        UnKnow = 23,
        Inspect = 24,
        WinRaid = 25,
        FlowerQuest = 26,
        Act = 255
    }
}