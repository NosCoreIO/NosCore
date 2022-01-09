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
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using Serilog;

namespace NosCore.PacketHandlers.Tests.Bazaar
{
    [TestClass]
    public class CBuyPacketHandlerTest
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Mock<IBazaarHttpClient>? _bazaarHttpClient;
        private CBuyPacketHandler? _cbuyPacketHandler;
        private Mock<IDao<IItemInstanceDto?, Guid>>? _itemInstanceDao;
        private Mock<IItemGenerationService>? _itemProvider;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            Broadcaster.Reset();
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _bazaarHttpClient = new Mock<IBazaarHttpClient>();
            _itemInstanceDao = new Mock<IDao<IItemInstanceDto?, Guid>>();
            _itemProvider = new Mock<IItemGenerationService>();
            _cbuyPacketHandler = new CBuyPacketHandler(_bazaarHttpClient.Object, _itemProvider.Object, Logger,
                _itemInstanceDao.Object, TestHelpers.Instance.LogLanguageLocalizer);

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
            var lastpacket = (ModaliPacket?)_session!.LastPackets.FirstOrDefault(s => s is ModaliPacket);
            Assert.IsTrue(lastpacket?.Type == 1 && lastpacket?.Message == Game18NConstString.OfferUpdated);
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
            var lastpacket = (ModaliPacket?)_session!.LastPackets.FirstOrDefault(s => s is ModaliPacket);
            Assert.IsTrue(lastpacket?.Type == 1 && lastpacket?.Message == Game18NConstString.OfferUpdated);
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
            var lastpacket = (ModaliPacket?)_session!.LastPackets.FirstOrDefault(s => s is ModaliPacket);
            Assert.IsTrue(lastpacket?.Type == 1 && lastpacket?.Message == Game18NConstString.OfferUpdated);
        }

        [TestMethod]
        public async Task BuyWhenCanNotAddItemAsync()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            _session!.Character.InventoryService!.AddItemToPocket(new InventoryItemInstance(new ItemInstance(new Item { VNum = 1012 }) { Amount = 999, Id = guid2 })
            {
                Id = guid2, Slot = 0, Type = NoscorePocketType.Main
            });
            _session.Character.InventoryService.AddItemToPocket(new InventoryItemInstance(new ItemInstance(new Item { VNum = 1012 }) { Amount = 999, Id = guid1 })
            {
                Id = guid1, Slot = 1, Type = NoscorePocketType.Main
            });
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 0,
                Price = 50,
                Amount = 1,
                VNum = 1012
            }, _session).ConfigureAwait(false);
            var lastpacket = (InfoiPacket?)_session.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.IsTrue(lastpacket?.Message == Game18NConstString.NotEnoughSpace);
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
            var lastpacket = (ModaliPacket?)_session!.LastPackets.FirstOrDefault(s => s is ModaliPacket);
            Assert.IsTrue(lastpacket?.Type == 1 && lastpacket?.Message == Game18NConstString.OfferUpdated);
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
            var item = new Item { Type = NoscorePocketType.Main, VNum = 1012 };
            _itemProvider!.Setup(s => s.Convert(It.IsAny<IItemInstanceDto>()))
                .Returns(new ItemInstance(item) { Amount = 1, Item = item });
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 3,
                Price = 50,
                Amount = 99,
                VNum = 1012
            }, _session).ConfigureAwait(false);
            var lastpacket = (SayiPacket?)_session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(lastpacket?.VisualType == VisualType.Player && lastpacket?.VisualId == _session.Character.CharacterId && lastpacket?.Type == SayColorType.Yellow && 
                lastpacket?.Message == Game18NConstString.BoughtItem && lastpacket?.ArgumentType == 2 && (string?)lastpacket?.Game18NArguments[0] == item.VNum.ToString() && (short?)lastpacket?.Game18NArguments[1] == 99);
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
            var item = new Item { Type = NoscorePocketType.Main, VNum = 1012 };
            _itemProvider!.Setup(s => s.Convert(It.IsAny<IItemInstanceDto>()))
                .Returns(new ItemInstance(item) { Amount = 1, Item = item });
            await _cbuyPacketHandler!.ExecuteAsync(new CBuyPacket
            {
                BazaarId = 0,
                Price = 50,
                Amount = 1,
                VNum = 1012
            }, _session).ConfigureAwait(false);
            var lastpacket = (SayiPacket?)_session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(lastpacket?.VisualType == VisualType.Player && lastpacket?.VisualId == _session.Character.CharacterId && lastpacket?.Type == SayColorType.Yellow &&
                lastpacket?.Message == Game18NConstString.BoughtItem && lastpacket?.ArgumentType == 2 && (string?)lastpacket?.Game18NArguments[0] == item.VNum.ToString() && (short?)lastpacket?.Game18NArguments[1] == 1);
        }
    }
}