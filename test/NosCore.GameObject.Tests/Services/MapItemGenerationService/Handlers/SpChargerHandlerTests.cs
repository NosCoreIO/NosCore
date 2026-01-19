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

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapItemGenerationService.Handlers;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.MapItemGenerationService.Handlers
{
    [TestClass]
    public class SpChargerHandlerTests
    {
        private SpChargerEventHandler Handler = null!;
        private ClientSession Session = null!;
        private IItemGenerationService ItemProvider = null!;
        private IIdService<MapItem> IdService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new SpChargerEventHandler();
            ItemProvider = TestHelpers.Instance.GenerateItemProvider();
            IdService = new IdService<MapItem>(1);
        }

        [TestMethod]
        public void ConditionShouldReturnFalseForNonSpCharger()
        {
            new Spec("Condition should return false for non-SP charger item")
                .Given(ItemIsNotSpCharger)
                .When(CheckingCondition)
                .Then(ConditionShouldBeFalse)
                .Execute();
        }

        [TestMethod]
        public void ConditionShouldReturnFalseForRegularItem()
        {
            new Spec("Condition should return false for regular item")
                .Given(ItemIsRegularMainItem)
                .When(CheckingCondition)
                .Then(ConditionShouldBeFalse)
                .Execute();
        }

        [TestMethod]
        public void ConditionShouldReturnFalseForEquipment()
        {
            new Spec("Condition should return false for equipment")
                .Given(ItemIsEquipment)
                .When(CheckingCondition)
                .Then(ConditionShouldBeFalse)
                .Execute();
        }

        private MapItem? DroppedSpCharger;
        private IItemInstance? ItemInstance;
        private bool ConditionResult;

        private void ItemIsNotSpCharger()
        {
            ItemInstance = ItemProvider.Create(1012, 10);
            DroppedSpCharger = CreateMapItem(ItemInstance);
        }

        private void ItemIsRegularMainItem()
        {
            ItemInstance = ItemProvider.Create(1013, 5);
            DroppedSpCharger = CreateMapItem(ItemInstance);
        }

        private void ItemIsEquipment()
        {
            ItemInstance = ItemProvider.Create(1, 1);
            DroppedSpCharger = CreateMapItem(ItemInstance);
        }

        private void CheckingCondition()
        {
            ConditionResult = Handler.Condition(DroppedSpCharger!);
        }

        private void ConditionShouldBeFalse()
        {
            Assert.IsFalse(ConditionResult);
        }

        private MapItem CreateMapItem(IItemInstance item)
        {
            var mapItem = new MapItem(IdService.GetNextId())
            {
                MapInstance = Session.Character.MapInstance,
                PositionX = 1,
                PositionY = 1,
                ItemInstance = item
            };
            return mapItem;
        }
    }
}
