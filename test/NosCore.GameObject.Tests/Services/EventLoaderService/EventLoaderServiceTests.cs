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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.EventLoaderService
{
    [TestClass]
    public class EventLoaderServiceTests
    {
        private IEventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>> Service = null!;
        private List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>> Handlers = null!;

        [TestInitialize]
        public void Setup()
        {
            Handlers = new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>();
            Service = new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(Handlers);
        }

        [TestMethod]
        public async Task ServiceCanBeConstructed()
        {
            await new Spec("Service can be constructed")
                .Then(ServiceShouldNotBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ServiceImplementsInterface()
        {
            await new Spec("Service implements interface")
                .Then(ServiceShouldImplementInterface)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoadHandlersWithNoHandlersShouldComplete()
        {
            await new Spec("Load handlers with no handlers should complete")
                .Given(ItemExists)
                .When(LoadingHandlers)
                .Then(RequestsShouldBeRegistered)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoadHandlersWithMatchingHandlerShouldRegisterHandler()
        {
            await new Spec("Load handlers with matching handler should register handler")
                .Given(ItemExists)
                .And(MatchingHandlerExists)
                .When(LoadingHandlers)
                .Then(RequestsShouldBeRegistered)
                .ExecuteAsync();
        }

        private Item? TestItem;

        private void ItemExists()
        {
            TestItem = new Item { VNum = 1 };
        }

        private void MatchingHandlerExists()
        {
            var handler = new Mock<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>();
            handler.Setup(h => h.Condition(It.IsAny<Item>())).Returns(true);
            Handlers.Add(handler.Object);
            Service = new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(Handlers);
        }

        private void LoadingHandlers()
        {
            Service.LoadHandlers(TestItem!);
        }

        private void ServiceShouldNotBeNull()
        {
            Assert.IsNotNull(Service);
        }

        private void ServiceShouldImplementInterface()
        {
            Assert.IsInstanceOfType(Service, typeof(IEventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>>));
        }

        private void RequestsShouldBeRegistered()
        {
            Assert.IsTrue(TestItem!.Requests.ContainsKey(typeof(IUseItemEventHandler)));
        }
    }
}
