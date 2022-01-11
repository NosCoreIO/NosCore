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
using System.Threading.Tasks;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.MpService;
using NosCore.Algorithm.ReputationService;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.HttpClients.PacketHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.SaveService;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Networking.SessionRef;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using Serilog;

namespace NosCore.GameObject.Tests
{
    [TestClass]
    public class ShopTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private IFriendHttpClient? _friendHttpClient;
        private IMapInstanceAccessorService? _instanceProvider;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _friendHttpClient = new Mock<IFriendHttpClient>().Object;
            TestHelpers.Instance.WorldConfiguration.Value.BackpackSize = 3;
            _instanceProvider = TestHelpers.Instance.MapInstanceAccessorService;
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
        }


        [TestMethod]
        public async Task UserCanNotShopNonExistingSlotAsync()
        {
            _session!.Character.Gold = 9999999999;
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 500000}
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem { Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0 });
            var shop = new Shop
            {
                ShopItems = list
            };
            await _session.Character.BuyAsync(shop, 1, 99).ConfigureAwait(false);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public async Task UserCantShopMoreThanQuantityNonExistingSlotAsync()
        {
            _session!.Character.Gold = 9999999999;
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 500000}
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem { Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0, Amount = 98 });
            var shop = new Shop
            {
                ShopItems = list
            };
            await _session.Character.BuyAsync(shop, 0, 99).ConfigureAwait(false);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public async Task UserCantShopWithoutMoneyAsync()
        {
            _session!.Character.Gold = 500000;
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 500000}
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem { Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0 });
            var shop = new Shop
            {
                ShopItems = list
            };
            await _session.Character.BuyAsync(shop, 0, 99).ConfigureAwait(false);

            var packet = (SMemoiPacket?)_session.LastPackets.FirstOrDefault(s => s is SMemoiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.NotEnoughGold5);
        }

        [TestMethod]
        public async Task UserCantShopWithoutReputAsync()
        {
            _session!.Character.Reput = 500000;
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, ReputPrice = 500000}
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem { Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0 });
            var shop = new Shop
            {
                ShopItems = list
            };
            await _session.Character.BuyAsync(shop, 0, 99).ConfigureAwait(false);

            var packet = (SMemoiPacket?)_session.LastPackets.FirstOrDefault(s => s is SMemoiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.ReputationNotHighEnough);
        }

        [TestMethod]
        public async Task UserCantShopWithoutPlaceAsync()
        {
            _session!.Character.Gold = 500000;

            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 1}
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.ItemProvider = itemBuilder;
            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem { Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0 });
            var shop = new Shop
            {
                ShopItems = list
            };
            _session!.Character.InventoryService!.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 0);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 2);

            await _session.Character.BuyAsync(shop, 0, 999).ConfigureAwait(false);
            var packet = (MsgiPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.NotEnoughSpace);
        }

        [TestMethod]
        public async Task UserCanShopAsync()
        {
            _session!.Character.Gold = 500000;

            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 1}
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.ItemProvider = itemBuilder;
            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem { Slot = 0, ItemInstance = itemBuilder.Create(1, -1), Type = 0 });
            var shop = new Shop
            {
                ShopItems = list
            };
            _session!.Character.InventoryService!.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 0);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 1), _session.Character.CharacterId),
                NoscorePocketType.Etc, 2);

            await _session.Character.BuyAsync(shop, 0, 998).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.ItemInstance?.Amount == 999));
            Assert.IsTrue(_session.Character.Gold == 499002);
        }

        [TestMethod]
        public async Task UserCanShopReputAsync()
        {
            _session!.Character.Reput = 500000;

            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, ReputPrice = 1}
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.ItemProvider = itemBuilder;
            var list = new ConcurrentDictionary<int, ShopItem>();
            list.TryAdd(0, new ShopItem { Slot = 0, ItemInstance = itemBuilder.Create(1), Type = 0 });
            var shop = new Shop
            {
                ShopItems = list
            };
            _session.Character.InventoryService!.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 0);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), _session.Character.CharacterId),
                NoscorePocketType.Etc, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 1), _session.Character.CharacterId),
                NoscorePocketType.Etc, 2);

            await _session.Character.BuyAsync(shop, 0, 998).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.ItemInstance?.Amount == 999));
            Assert.IsTrue(_session.Character.Reput == 499002);
        }

        private async Task<ClientSession> PrepareSessionShopAsync()
        {
            var conf = Options.Create(new WorldConfiguration { BackpackSize = 3, MaxItemAmount = 999, MaxGoldAmount = 999_999_999 });
            var session2 = new ClientSession(conf, new Mock<IExchangeService>().Object, Logger, new List<IPacketHandler>(), _friendHttpClient!, new Mock<ISerializer>().Object, new Mock<IPacketHttpClient>().Object, new Mock<IMinilandService>().Object, TestHelpers.Instance.MapInstanceGeneratorService, new SessionRefHolder(), new Mock<ISaveService>().Object, new Mock<ILogLanguageLocalizer<NosCore.Networking.Resource.LogLanguageKey>>().Object, TestHelpers.Instance.LogLanguageLocalizer);
            var channelMock = new Mock<ISocketChannel>();
            session2.RegisterChannel(channelMock.Object);
            var account = new AccountDto { Name = "AccountTest", Password = new Sha512Hasher().Hash("test") };
            session2.InitializeAccount(account);
            session2.SessionId = 1;

            await session2.SetCharacterAsync(new Character(new InventoryService(new List<ItemDto>(), conf, Logger), new Mock<IExchangeService>().Object, new Mock<IItemGenerationService>().Object,
                Logger, new HpService(), new MpService(), new ExperienceService(), new JobExperienceService(), new HeroExperienceService(), new ReputationService(), new DignityService(), TestHelpers.Instance.WorldConfiguration, new Mock<ISpeedCalculationService>().Object)
            {
                CharacterId = 1,
                Name = "chara2",
                Slot = 1,
                AccountId = 1,
                MapId = 1,
                State = CharacterState.Active
            }).ConfigureAwait(false);
            var mapinstance = _instanceProvider!.GetBaseMapById(0)!;
            session2.Account = account;
            session2.Character.MapInstance = _instanceProvider.GetBaseMapById(0)!;
            session2.Character.MapInstance = mapinstance;

            _session!.Character.Gold = 500000;
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1, IsSoldable = true, Price = 1}
            };
            var itemBuilder = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.ItemProvider = itemBuilder;
            var list = new ConcurrentDictionary<int, ShopItem>();
            var it = itemBuilder.Create(1, 999);
            session2.Character.InventoryService!.AddItemToPocket(
                InventoryItemInstance.Create(it, session2.Character.CharacterId), NoscorePocketType.Etc, 0);
            list.TryAdd(0, new ShopItem { Slot = 0, ItemInstance = it, Type = 0, Price = 1, Amount = 999 });
            list.TryAdd(1, new ShopItem { Slot = 1, ItemInstance = it, Type = 0, Price = 1, Amount = 500 });
            session2.Character.Shop = new Shop
            {
                Session = session2,
                ShopItems = list
            };
            _session.Character.InventoryService!.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), session2.Character.CharacterId),
                NoscorePocketType.Etc, 0);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(itemBuilder.Create(1, 999), session2.Character.CharacterId),
                NoscorePocketType.Etc, 1);
            return session2;
        }

        [TestMethod]
        public async Task UserCanShopFromSessionAsync()
        {
            var session2 = await PrepareSessionShopAsync().ConfigureAwait(false);
            await _session!.Character.BuyAsync(session2.Character.Shop!, 0, 999).ConfigureAwait(false);
            Assert.IsTrue(session2.Character.Gold == 999);
            Assert.IsTrue(session2.Character.InventoryService!.CountItem(1) == 0);
        }

        [TestMethod]
        public async Task UserCanShopFromSessionPartialAsync()
        {
            var session2 = await PrepareSessionShopAsync().ConfigureAwait(false);
            await _session!.Character.BuyAsync(session2.Character.Shop!, 0, 998).ConfigureAwait(false);
            Assert.IsTrue(session2.Character.Gold == 998);
            Assert.IsTrue(session2.Character.InventoryService!.CountItem(1) == 1);
        }

        [TestMethod]
        public async Task UserCanNotShopMoreThanShopAsync()
        {
            var session2 = await PrepareSessionShopAsync().ConfigureAwait(false);
            await _session!.Character.BuyAsync(session2.Character.Shop!, 1, 501).ConfigureAwait(false);
            Assert.IsTrue(session2.Character.Gold == 0);
            Assert.IsTrue(session2.Character.InventoryService!.CountItem(1) == 999);
        }

        [TestMethod]
        public async Task UserCanShopFullAsync()
        {
            var session2 = await PrepareSessionShopAsync().ConfigureAwait(false);
            await _session!.Character.BuyAsync(session2.Character.Shop!, 1, 500).ConfigureAwait(false);
            Assert.IsTrue(session2.Character.Gold == 500);
            Assert.IsTrue(session2.Character.InventoryService!.CountItem(1) == 499);
        }

        [TestMethod]
        public async Task UserCanNotShopTooRichAsync()
        {
            var session2 = await PrepareSessionShopAsync().ConfigureAwait(false);
            session2.Character.Gold = 999_999_999;
            await _session!.Character.BuyAsync(session2.Character.Shop!, 0, 999).ConfigureAwait(false);
            Assert.IsTrue(session2.Character.Gold == 999_999_999);
            Assert.IsTrue(session2.Character.InventoryService!.CountItem(1) == 999);
            var packet = (SMemoPacket?)_session.LastPackets.FirstOrDefault(s => s is SMemoPacket);
            Assert.IsTrue(packet?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.TOO_RICH_SELLER, _session.Account.Language));
        }
    }
}