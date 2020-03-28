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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using DotNetty.Transport.Channels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.InventoryTests
{
    [TestClass]
    public class ShopTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private IFriendHttpClient _friendHttpClient;
        private MapInstanceProvider _instanceProvider;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            Broadcaster.Reset();
            TestHelpers.Reset();
            _friendHttpClient = new Mock<IFriendHttpClient>().Object;
            TestHelpers.Instance.WorldConfiguration.BackpackSize = 3;
            _instanceProvider = TestHelpers.Instance.MapInstanceProvider;
            _session = TestHelpers.Instance.GenerateSession();
        }


        [TestMethod]
        public void UserCanNotShopNonExistingSlot()
        {
            _session.Character.Gold = 9999999999;
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 500000}
            };
            var itemBuilder = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());

            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem {Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0});
            var shop = new Shop
            {
                ShopItems = list
            };
            _session.Character.Buy(shop, 1, 99);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public void UserCantShopMoreThanQuantityNonExistingSlot()
        {
            _session.Character.Gold = 9999999999;
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 500000}
            };
            var itemBuilder = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());

            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem {Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0, Amount = 98});
            var shop = new Shop
            {
                ShopItems = list
            };
            _session.Character.Buy(shop, 0, 99);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public void UserCantShopWithoutMoney()
        {
            _session.Character.Gold = 500000;
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 500000}
            };
            var itemBuilder = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());

            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem {Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0});
            var shop = new Shop
            {
                ShopItems = list
            };
            _session.Character.Buy(shop, 0, 99);

            var packet = (SMemoPacket) _session.LastPackets.FirstOrDefault(s => s is SMemoPacket);
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, _session.Account.Language));
        }

        [TestMethod]
        public void UserCantShopWithoutReput()
        {
            _session.Character.Reput = 500000;
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, ReputPrice = 500000}
            };
            var itemBuilder = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());

            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem {Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0});
            var shop = new Shop
            {
                ShopItems = list
            };
            _session.Character.Buy(shop, 0, 99);

            var packet = (SMemoPacket) _session.LastPackets.FirstOrDefault(s => s is SMemoPacket);
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_REPUT, _session.Account.Language));
        }

        [TestMethod]
        public void UserCantShopWithoutPlace()
        {
            _session.Character.Gold = 500000;

            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 1}
            };
            var itemBuilder = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
            _session.Character.ItemProvider = itemBuilder;
            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem {Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0});
            var shop = new Shop
            {
                ShopItems = list
            };
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 0);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 2);

            _session.Character.Buy(shop, 0, 999);
            var packet = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE, _session.Account.Language));
        }

        [TestMethod]
        public void UserCanShop()
        {
            _session.Character.Gold = 500000;

            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 1}
            };
            var itemBuilder = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
            _session.Character.ItemProvider = itemBuilder;
            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem {Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0});
            var shop = new Shop
            {
                ShopItems = list
            };
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 0);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 1), _session.Character.CharacterId),
                NoscorePocketType.Etc, 2);

            _session.Character.Buy(shop, 0, 998);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.ItemInstance.Amount == 999));
            Assert.IsTrue(_session.Character.Gold == 499002);
        }

        [TestMethod]
        public void UserCanShopReput()
        {
            _session.Character.Reput = 500000;

            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, ReputPrice = 1}
            };
            var itemBuilder = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
            _session.Character.ItemProvider = itemBuilder;
            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem {Slot = 0, ItemInstance = itemBuilder.Create(1), Type = 0});
            var shop = new Shop
            {
                ShopItems = list
            };
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 0);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 1), _session.Character.CharacterId),
                NoscorePocketType.Etc, 2);

            _session.Character.Buy(shop, 0, 998);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.ItemInstance.Amount == 999));
            Assert.IsTrue(_session.Character.Reput == 499002);
        }

        private ClientSession PrepareSessionShop()
        {
            var conf = new WorldConfiguration {BackpackSize = 3, MaxItemAmount = 999, MaxGoldAmount = 999_999_999};
            var session2 = new ClientSession(conf, _logger, new List<IPacketHandler>(), _friendHttpClient, null, null);
            var channelMock = new Mock<IChannel>();
            session2.RegisterChannel(channelMock.Object);
            var account = new AccountDto {Name = "AccountTest", Password = "test".ToSha512()};
            session2.InitializeAccount(account);
            session2.SessionId = 1;

            session2.SetCharacter(new Character(new InventoryService(new List<ItemDto>(), conf, _logger), null, null,
                null, null, null, null, _logger, null, null, null, null, null)
            {
                CharacterId = 1,
                Name = "chara2",
                Slot = 1,
                AccountId = 1,
                MapId = 1,
                State = CharacterState.Active
            });
            var mapinstance = _instanceProvider.GetBaseMapById(0);
            session2.Character.Account = account;
            session2.Character.MapInstance = _instanceProvider.GetBaseMapById(0);
            session2.Character.MapInstance = mapinstance;

            _session.Character.Gold = 500000;
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 1}
            };
            var itemBuilder = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
            _session.Character.ItemProvider = itemBuilder;
            var list = new ConcurrentDictionary<int, ShopItem>();
            var it = itemBuilder.Create(1, 999);
            session2.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(it, session2.Character.CharacterId), NoscorePocketType.Etc, 0);
            list.TryAdd(0, new ShopItem {Slot = 0, ItemInstance = it, Type = 0, Price = 1, Amount = 999});
            list.TryAdd(1, new ShopItem {Slot = 1, ItemInstance = it, Type = 0, Price = 1, Amount = 500});
            session2.Character.Shop = new Shop
            {
                Session = session2,
                ShopItems = list
            };
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), session2.Character.CharacterId),
                NoscorePocketType.Etc, 0);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), session2.Character.CharacterId),
                NoscorePocketType.Etc, 1);
            return session2;
        }

        [TestMethod]
        public void UserCanShopFromSession()
        {
            var session2 = PrepareSessionShop();
            _session.Character.Buy(session2.Character.Shop, 0, 999);
            Assert.IsTrue(session2.Character.Gold == 999);
            Assert.IsTrue(session2.Character.InventoryService.CountItem(1) == 0);
        }

        [TestMethod]
        public void UserCanShopFromSessionPartial()
        {
            var session2 = PrepareSessionShop();
            _session.Character.Buy(session2.Character.Shop, 0, 998);
            Assert.IsTrue(session2.Character.Gold == 998);
            Assert.IsTrue(session2.Character.InventoryService.CountItem(1) == 1);
        }

        [TestMethod]
        public void UserCanNotShopMoreThanShop()
        {
            var session2 = PrepareSessionShop();
            _session.Character.Buy(session2.Character.Shop, 1, 501);
            Assert.IsTrue(session2.Character.Gold == 0);
            Assert.IsTrue(session2.Character.InventoryService.CountItem(1) == 999);
        }

        [TestMethod]
        public void UserCanShopFull()
        {
            var session2 = PrepareSessionShop();
            _session.Character.Buy(session2.Character.Shop, 1, 500);
            Assert.IsTrue(session2.Character.Gold == 500);
            Assert.IsTrue(session2.Character.InventoryService.CountItem(1) == 499);
        }

        [TestMethod]
        public void UserCanNotShopTooRich()
        {
            var session2 = PrepareSessionShop();
            session2.Character.Gold = 999_999_999;
            _session.Character.Buy(session2.Character.Shop, 0, 999);
            Assert.IsTrue(session2.Character.Gold == 999_999_999);
            Assert.IsTrue(session2.Character.InventoryService.CountItem(1) == 999);
            var packet = (SMemoPacket) _session.LastPackets.FirstOrDefault(s => s is SMemoPacket);
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.TOO_RICH_SELLER, _session.Account.Language));
        }
    }
}