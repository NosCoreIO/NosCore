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
using NosCore.Data.Enumerations.Group;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

#pragma warning disable 618

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class MShopPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private readonly MShopPacket ShopPacket = new()
        {
            Type = CreateShopPacketType.Open,
            ItemList = new List<MShopItemSubPacket?>
            {
                new() { Type = PocketType.Etc, Slot = 0, Amount = 1, Price = 10000 },
                new() { Type = PocketType.Etc, Slot = 1, Amount = 2, Price = 20000 },
                new() { Type = PocketType.Etc, Slot = 2, Amount = 3, Price = 30000 }
            },
            Name = "TEST SHOP"
        };

        private MShopPacketHandler MShopPacketHandler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();

            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.MapInstance.Portals = new List<Portal>
            {
                new()
                {
                    DestinationMapId = Session.Character.MapInstance.Map.MapId,
                    Type = PortalType.Open,
                    SourceMapInstanceId = Session.Character.MapInstance.MapInstanceId,
                    DestinationMapInstanceId = Session.Character.MapInstance.MapInstanceId,
                    DestinationX = 5,
                    DestinationY = 5,
                    PortalId = 1,
                    SourceMapId = Session.Character.MapInstance.Map.MapId,
                    SourceX = 0,
                    SourceY = 0
                }
            };
            MShopPacketHandler = new MShopPacketHandler(TestHelpers.Instance.DistanceCalculator);
        }

        [TestMethod]
        public async Task UserCannotCreateShopCloseToPortal()
        {
            await new Spec("User cannot create shop close to portal")
                .WhenAsync(CreatingShopAtPortal)
                .Then(ShouldReceiveOpenAwayFromPortalError)
                .And(ShopShouldNotBeCreated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCannotCreateShopInTeam()
        {
            await new Spec("User cannot create shop in team")
                .Given(CharacterIsInTeam)
                .WhenAsync(CreatingShop)
                .Then(ShouldReceiveTeammateCannotShopError)
                .And(ShopShouldNotBeCreated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCanCreateShopInGroup()
        {
            await new Spec("User can create shop in group")
                .Given(CharacterIsInGroup)
                .WhenAsync(CreatingShop)
                .Then(NoErrorShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCannotCreateShopInNonShopAllowedMap()
        {
            await new Spec("User cannot create shop in non shop allowed map")
                .Given(CharacterIsAwayFromPortal)
                .WhenAsync(CreatingShop)
                .Then(ShouldReceiveUseCommercialMapError)
                .And(ShopShouldNotBeCreated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCannotCreateShopWithMissingItem()
        {
            await new Spec("User cannot create shop with missing item")
                .Given(CharacterHasOnlyOneItem)
                .WhenAsync(CreatingShop)
                .Then(ShopShouldNotBeCreated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCannotCreateShopWithMissingAmount()
        {
            await new Spec("User cannot create shop with missing amount")
                .Given(CharacterHasItemsWithAmount_, 1)
                .WhenAsync(CreatingShop)
                .Then(ShouldReceiveItemsCannotBeTradedError)
                .And(ShopShouldNotBeCreated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCanCreateShop()
        {
            await new Spec("User can create shop")
                .Given(CharacterHasTradableItems)
                .WhenAsync(CreatingShop)
                .Then(ShopShouldBeCreated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCannotCreateShopInExchange()
        {
            await new Spec("User cannot create shop in exchange")
                .Given(CharacterIsInExchange)
                .And(CharacterHasTradableItems)
                .WhenAsync(CreatingShopViaPacket)
                .Then(ShopShouldNotBeCreated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UserCannotCreateEmptyShop()
        {
            await new Spec("User cannot create empty shop")
                .Given(CharacterIsOnShopAllowedMap)
                .WhenAsync(CreatingEmptyShop)
                .Then(ShouldReceiveNoItemToSellError)
                .And(ShopShouldNotBeCreated)
                .ExecuteAsync();
        }

        private void CharacterIsInTeam()
        {
            Session.Character.PositionX = 7;
            Session.Character.PositionY = 7;
            Session.Character.Group = new NosCore.GameObject.Services.GroupService.Group(GroupType.Team, new Mock<ISessionGroupFactory>().Object);
        }

        private void CharacterIsInGroup()
        {
            Session.Character.PositionX = 7;
            Session.Character.PositionY = 7;
            Session.Character.Group = new NosCore.GameObject.Services.GroupService.Group(GroupType.Group, new Mock<ISessionGroupFactory>().Object);
        }

        private void CharacterIsAwayFromPortal()
        {
            Session.Character.PositionX = 7;
            Session.Character.PositionY = 7;
        }

        private void CharacterHasOnlyOneItem()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = 1 }
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, 1), 0));
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void CharacterHasItemsWithAmount_(int value)
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Etc, VNum = 1 }
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, (short)value), 0),
                NoscorePocketType.Etc, 0);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, (short)value), 0),
                NoscorePocketType.Etc, 1);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemBuilder.Create(1, (short)value), 0),
                NoscorePocketType.Etc, 2);

            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
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

            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void CharacterIsInExchange()
        {
            Session.Character.InShop = true;
        }

        private void CharacterIsOnShopAllowedMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task CreatingShopAtPortal()
        {
            await MShopPacketHandler.ExecuteAsync(ShopPacket, Session);
        }

        private async Task CreatingShop()
        {
            await MShopPacketHandler.ExecuteAsync(ShopPacket, Session);
        }

        private async Task CreatingShopViaPacket()
        {
            await Session.HandlePacketsAsync(new[] { ShopPacket });
        }

        private async Task CreatingEmptyShop()
        {
            await MShopPacketHandler.ExecuteAsync(new MShopPacket
            {
                Type = CreateShopPacketType.Open,
                ItemList = new List<MShopItemSubPacket?>(),
                Name = "TEST SHOP"
            }, Session);
        }

        private void ShouldReceiveOpenAwayFromPortalError()
        {
            var packet = (InfoiPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.OpenShopAwayPortal);
        }

        private void ShouldReceiveTeammateCannotShopError()
        {
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player && packet?.VisualId == Session.Character.CharacterId && packet?.Type == SayColorType.Red && packet?.Message == Game18NConstString.TeammateCanNotOpenShop);
        }

        private void ShouldReceiveUseCommercialMapError()
        {
            var packet = (InfoiPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.UseCommercialMapToShop);
        }

        private void ShouldReceiveItemsCannotBeTradedError()
        {
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player && packet?.VisualId == Session.Character.CharacterId && packet?.Type == SayColorType.Red && packet?.Message == Game18NConstString.SomeItemsCannotBeTraded);
        }

        private void ShouldReceiveNoItemToSellError()
        {
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player && packet?.VisualId == Session.Character.CharacterId && packet?.Type == SayColorType.Yellow && packet?.Message == Game18NConstString.NoItemToSell);
        }

        private void NoErrorShouldBeSent()
        {
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsNull(packet);
        }

        private void ShopShouldNotBeCreated()
        {
            Assert.IsNull(Session.Character.Shop);
        }

        private void ShopShouldBeCreated()
        {
            Assert.IsNotNull(Session.Character.Shop);
        }
    }
}
