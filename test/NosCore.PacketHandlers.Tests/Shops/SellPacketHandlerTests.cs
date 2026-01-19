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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using NosCore.GameObject.Infastructure;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class SellPacketHandlerTests
    {
        private MapInstanceAccessorService InstanceProvider = null!;
        private SellPacketHandler SellPacketHandler = null!;
        private ClientSession Session = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            InstanceProvider = TestHelpers.Instance.MapInstanceAccessorService;
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            SellPacketHandler = new SellPacketHandler(TestHelpers.Instance.WorldConfiguration);
        }

        [TestMethod]
        public async Task UserCannotSellInExchange()
        {
            await new Spec("User cannot sell in exchange")
                .Given(CharacterIsInShop)
                .And(CharacterHasTradableItems)
                .WhenAsync(SellingItem)
                .Then(GoldShouldNotIncrease)
                .And(ItemShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCannotSellNonSoldableItem()
        {
            await new Spec("User cannot sell non soldable item")
                .Given(CharacterHasNonSoldableItems)
                .WhenAsync(SellingItem)
                .Then(ShouldReceiveCannotSellError)
                .And(GoldShouldNotIncrease)
                .And(ItemShouldStillExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCanSellItem()
        {
            await new Spec("User can sell item")
                .Given(CharacterHasSoldableItems)
                .WhenAsync(SellingItem)
                .Then(GoldShouldIncrease)
                .And(ItemShouldBeRemoved)
                .ExecuteAsync();
        }

        private void CharacterIsInShop()
        {
            Session.Character.InShop = true;
        }

        private void CharacterHasTradableItems()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = 1, IsTradable = true }
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 1), 0),
                NoscorePocketType.Etc, 0);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 2), 0),
                NoscorePocketType.Etc, 1);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 3), 0),
                NoscorePocketType.Etc, 2);

            Session.Character.MapInstance = InstanceProvider.GetBaseMapById(1)!;
        }

        private void CharacterHasNonSoldableItems()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = false }
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 1), 0),
                NoscorePocketType.Etc, 0);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 2), 0),
                NoscorePocketType.Etc, 1);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 3), 0),
                NoscorePocketType.Etc, 2);

            Session.Character.MapInstance = InstanceProvider.GetBaseMapById(1)!;
        }

        private void CharacterHasSoldableItems()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 500000 }
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 1), 0),
                NoscorePocketType.Etc, 0);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 2), 0),
                NoscorePocketType.Etc, 1);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 3), 0),
                NoscorePocketType.Etc, 2);

            Session.Character.MapInstance = InstanceProvider.GetBaseMapById(1)!;
        }

        private async Task SellingItem()
        {
            await SellPacketHandler.ExecuteAsync(new SellPacket { Slot = 0, Amount = 1, Data = (short)NoscorePocketType.Etc },
                Session);
        }

        private void GoldShouldNotIncrease()
        {
            Assert.IsTrue(Session.Character.Gold == 0);
        }

        private void GoldShouldIncrease()
        {
            Assert.IsTrue(Session.Character.Gold > 0);
        }

        private void ItemShouldStillExist()
        {
            Assert.IsNotNull(Session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Etc));
        }

        private void ItemShouldBeRemoved()
        {
            Assert.IsNull(Session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Etc));
        }

        private void ShouldReceiveCannotSellError()
        {
            var packet = (SMemoiPacket?)Session.LastPackets.FirstOrDefault(s => s is SMemoiPacket);
            Assert.IsTrue(packet?.Type == SMemoType.FailNpc && packet?.Message == Game18NConstString.ItemCanNotBeSold);
        }
    }
}
