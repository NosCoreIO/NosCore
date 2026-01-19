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

using System.Collections.Generic;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;

namespace NosCore.Tests.Shared.BDD.Steps
{
    public static class SessionSteps
    {
        public static void WithGold(this ClientSession session, long gold)
        {
            session.Character.Gold = gold;
        }

        public static void WithItem(this ClientSession session, IItemGenerationService itemProvider, short vnum, short amount = 1)
        {
            session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemProvider.Create(vnum, amount), 0));
        }

        public static void WithMedalBonus(this ClientSession session, StaticBonusType bonusType)
        {
            session.Character.StaticBonusList ??= new List<StaticBonusDto>();
            session.Character.StaticBonusList.Add(new StaticBonusDto { StaticBonusType = bonusType });
        }

        public static void InShop(this ClientSession session)
        {
            session.Character.InShop = true;
        }

        public static void WithLevel(this ClientSession session, byte level)
        {
            session.Character.Level = level;
        }

        public static void WithJobLevel(this ClientSession session, byte jobLevel)
        {
            session.Character.JobLevel = jobLevel;
        }
    }
}
