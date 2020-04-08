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
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CBuyPacketHandlerTest
    {
        private static readonly ILogger Logger = Core.I18N.Logger.GetLoggerConfiguration().CreateLogger();
        private Mock<IBazaarHttpClient>? _bazaarHttpClient;
        private CBuyPacketHandler? _cbuyPacketHandler;
        private Mock<IDao<IItemInstanceDto, Guid>>? _itemInstanceDao;
        private Mock<IItemProvider>? _itemProvider;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            TestHelpers.Reset();
            Broadcaster.Reset();
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _bazaarHttpClient = new Mock<IBazaarHttpClient>();
            _itemInstanceDao = new Mock<IDao<IItemInstanceDto, Guid>>();
            _itemProvider = new Mock<IItemProvider>();
            _cbuyPacketHandler = new CBuyPacketHandler(_bazaarHttpClient.Object, _itemProvider.Object, Logger,
                _itemInstanceDao.Object);

            _bazaarHttpClient.Setup(b => b.GetBazaarLinkAsync(0)).ReturnsAsync(
                new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto { Price = 50, Amount = 1 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 1 }
                });
            _bazaarHttpClient.Setup(b => b.GetBazaarLinkAsync(2)).ReturnsAsync(
                new BazaarLink
                {
                    SellerName = _session!.Character.Name,
                    BazaarItem = new BazaarItemDto { Price = 60, Amount = 1 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012 }
                });
            _bazaarHttpClient.Setup(b => b.GetBazaarLinkAsync(3)).ReturnsAsync(
                new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto { Price = 50, Amount = 99, IsPackage = true },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 99 }
                });
            _bazaarHttpClient.Setup(b => b.GetBazaarLinkAsync(1)).ReturnsAsync((BazaarLink?)null);
            _bazaarHttpClient.Setup(b => b.RemoveAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
        }

        [TestMethod]
        public async Task BuyWhenExchangeOrTradeAsync()
        {
            _session!.Character.InShop = true;
            await _session!.HandlePacketsAsync(new[]
            {
                new CBuyPacket
                {
                    BazaarId = 1,
                    Price = 50,
                    Amount = 1,
                    VNum = 1012
                }
            }).ConfigureAwait(false); ;
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public async Task BuyWhenNoItemFoundAsync()
        {
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 1,
                Price = 50,
                Amount = 1,
                VNum = 1012
            }, _session!).ConfigureAwait(false);
            var lastpacket = (ModalPacket?)_session!.LastPackets!.FirstOrDefault(s => s is ModalPacket);
            Assert.IsTrue(lastpacket?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, _session.Account.Language));
        }

        [TestMethod]
        public async Task BuyWhenSellerAsync()
        {
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 2,
                Price = 50,
                Amount = 1,
                VNum = 1012
            }, _session!).ConfigureAwait(false);
            var lastpacket = (ModalPacket?)_session!.LastPackets.FirstOrDefault(s => s is ModalPacket);
            Assert.IsTrue(lastpacket?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, _session.Account.Language));
        }

        [TestMethod]
        public async Task BuyWhenDifferentPriceAsync()
        {
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 0,
                Price = 40,
                Amount = 1,
                VNum = 1012
            }, _session!).ConfigureAwait(false);
            var lastpacket = (ModalPacket?)_session!.LastPackets.FirstOrDefault(s => s is ModalPacket);
            Assert.IsTrue(lastpacket?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, _session.Account.Language));
        }

        [TestMethod]
        public async Task BuyWhenCanNotAddItemAsync()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            _session!.Character.InventoryService!.AddItemToPocket(new InventoryItemInstance
            {
                Id = guid2, ItemInstanceId = guid2, Slot = 0, Type = NoscorePocketType.Main,
                ItemInstance = new ItemInstance { ItemVNum = 1012, Amount = 999, Id = guid2 }
            });
            _session.Character.InventoryService.AddItemToPocket(new InventoryItemInstance
            {
                Id = guid1, ItemInstanceId = guid1, Slot = 1, Type = NoscorePocketType.Main,
                ItemInstance = new ItemInstance { ItemVNum = 1012, Amount = 999, Id = guid1 }
            });
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 0,
                Price = 50,
                Amount = 1,
                VNum = 1012
            }, _session).ConfigureAwait(false);
            var lastpacket = (InfoPacket?)_session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.IsTrue(lastpacket?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE, _session.Account.Language));
        }

        [TestMethod]
        public async Task BuyMoreThanSellingAsync()
        {
            _session!.Character.Gold = 5000;
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 0,
                Price = 50,
                Amount = 2,
                VNum = 1012
            }, _session).ConfigureAwait(false);
            var lastpacket = (ModalPacket?)_session.LastPackets.FirstOrDefault(s => s is ModalPacket);
            Assert.IsTrue(lastpacket?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, _session.Account.Language));
        }

        [TestMethod]
        public async Task BuyPartialPackageAsync()
        {
            _session!.Character.Gold = 5000;
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 3,
                Price = 50,
                Amount = 1,
                VNum = 1012
            }, _session).ConfigureAwait(false);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public async Task BuyPackageAsync()
        {
            _session!.Character.Gold = 5000;
            var item = new Item { Type = NoscorePocketType.Main };
            _itemProvider!.Setup(s => s.Convert(It.IsAny<IItemInstanceDto>()))
                .Returns(new ItemInstance { Amount = 1, ItemVNum = 1012, Item = item });
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 3,
                Price = 50,
                Amount = 99,
                VNum = 1012
            }, _session).ConfigureAwait(false);
            var lastpacket = (SayPacket?)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue(lastpacket?.Message ==
                $"{GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, _session.Account.Language)}: {item.Name[_session.Account.Language]} x {99}");
        }

        [TestMethod]
        public async Task BuyNotEnoughMoneyAsync()
        {
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 0,
                Price = 50,
                Amount = 1,
                VNum = 1012
            }, _session!).ConfigureAwait(false);
            var lastpacket = (ModalPacket?)_session!.LastPackets.FirstOrDefault(s => s is ModalPacket);
            Assert.IsTrue(lastpacket?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, _session.Account.Language));
        }

        [TestMethod]
        public async Task BuyAsync()
        {
            _session!.Character.Gold = 5000;
            var item = new Item { Type = NoscorePocketType.Main };
            _itemProvider!.Setup(s => s.Convert(It.IsAny<IItemInstanceDto>()))
                .Returns(new ItemInstance { Amount = 1, ItemVNum = 1012, Item = item });
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 0,
                Price = 50,
                Amount = 1,
                VNum = 1012
            }, _session).ConfigureAwait(false);
            var lastpacket = (SayPacket?)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue(lastpacket?.Message ==
                $"{GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, _session.Account.Language)}: {item.Name[_session.Account.Language]} x {1}");
        }
    }
}