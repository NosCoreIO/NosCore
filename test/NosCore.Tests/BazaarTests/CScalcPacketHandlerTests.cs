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
using System.Linq.Expressions;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.ServerPackets.Bazaar;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CScalcPacketHandlerTest
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Mock<IBazaarHttpClient>? _bazaarHttpClient;
        private CScalcPacketHandler? _cScalcPacketHandler;
        private Mock<IDao<IItemInstanceDto?, Guid>>? _itemInstanceDao;
        private Mock<IItemProvider>? _itemProvider;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            Broadcaster.Reset();
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _bazaarHttpClient = new Mock<IBazaarHttpClient>();
            _itemProvider = new Mock<IItemProvider>();
            _itemInstanceDao = new Mock<IDao<IItemInstanceDto?, Guid>>();
            _cScalcPacketHandler = new CScalcPacketHandler(TestHelpers.Instance.WorldConfiguration,
                _bazaarHttpClient.Object, _itemProvider.Object, Logger, _itemInstanceDao.Object);

            _bazaarHttpClient.Setup(b => b.GetBazaarLinkAsync(0)).ReturnsAsync(
                new BazaarLink
                {
                    SellerName = _session.Character.Name,
                    BazaarItem = new BazaarItemDto {Price = 50, Amount = 1},
                    ItemInstance = new ItemInstanceDto {ItemVNum = 1012, Amount = 0}
                });
            _bazaarHttpClient.Setup(b => b.GetBazaarLinkAsync(2)).ReturnsAsync(
                new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto {Price = 60, Amount = 1},
                    ItemInstance = new ItemInstanceDto {ItemVNum = 1012, Amount = 0}
                });
            _bazaarHttpClient.Setup(b => b.GetBazaarLinkAsync(1)).ReturnsAsync((BazaarLink?) null);
            _bazaarHttpClient.Setup(b => b.RemoveAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
            _itemProvider.Setup(s => s.Convert(It.IsAny<IItemInstanceDto>())).Returns(new ItemInstance
                {Amount = 0, ItemVNum = 1012, Item = new Item()});
        }

        [TestMethod]
        public async Task RetrieveWhenInExchangeOrTradeAsync()
        {
            _session!.Character.InShop = true;
            await _session!.HandlePacketsAsync(new[]
            {
                new CScalcPacket
                {
                    BazaarId = 1,
                    Price = 50,
                    Amount = 1,
                    VNum = 1012
                }
            }).ConfigureAwait(false);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public async Task RetrieveWhenNoItemAsync()
        {
            await _cScalcPacketHandler!.ExecuteAsync(new CScalcPacket
            {
                BazaarId = 1,
                Price = 50,
                Amount = 1,
                VNum = 1012
            }, _session!).ConfigureAwait(false);
            var lastpacket = (RCScalcPacket?) _session!.LastPackets.FirstOrDefault(s => s is RCScalcPacket);
            Assert.AreEqual(0, lastpacket?.Price);
        }

        [TestMethod]
        public async Task RetrieveWhenNotYourItemAsync()
        {
           await _cScalcPacketHandler!.ExecuteAsync(new CScalcPacket
           {
               BazaarId = 2,
               Price = 50,
               Amount = 1,
               VNum = 1012
           }, _session!).ConfigureAwait(false);
            var lastpacket = (RCScalcPacket?) _session!.LastPackets.FirstOrDefault(s => s is RCScalcPacket);
            Assert.AreEqual(0, lastpacket?.Price);
        }

        [TestMethod]
        public async Task RetrieveWhenNotEnoughPlaceAsync()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            _session!.Character.InventoryService!.AddItemToPocket(new InventoryItemInstance
            {
                Id = guid2, ItemInstanceId = guid2, Slot = 0, Type = NoscorePocketType.Main,
                ItemInstance = new ItemInstance {ItemVNum = 1012, Amount = 999, Id = guid2}
            });
            _session.Character.InventoryService.AddItemToPocket(new InventoryItemInstance
            {
                Id = guid1, ItemInstanceId = guid1, Slot = 1, Type = NoscorePocketType.Main,
                ItemInstance = new ItemInstance {ItemVNum = 1012, Amount = 999, Id = guid1}
            });
           await _cScalcPacketHandler!.ExecuteAsync(new CScalcPacket
           {
               BazaarId = 0,
               Price = 50,
               Amount = 1,
               VNum = 1012
           }, _session).ConfigureAwait(false);
            var lastpacket = (RCScalcPacket?) _session.LastPackets.FirstOrDefault(s => s is RCScalcPacket);
            Assert.AreEqual(50, lastpacket?.Price);
        }

        [TestMethod]
        public async Task RetrieveWhenMaxGoldAsync()
        {
            _session!.Character.Gold = TestHelpers.Instance.WorldConfiguration.Value.MaxGoldAmount;
            await _cScalcPacketHandler!.ExecuteAsync(new CScalcPacket
            {
                BazaarId = 0,
                Price = 50,
                Amount = 1,
                VNum = 1012
            }, _session).ConfigureAwait(false);
            var lastpacket = (MsgiPacket?) _session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(Game18NConstString.MaxGoldReached, lastpacket?.Message);
        }


        [TestMethod]
        public async Task RetrieveAsync()
        {
            _itemInstanceDao!.Setup(s=>s.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?,bool>>>()))
                .ReturnsAsync(new ItemInstanceDto { ItemVNum = 1012, Amount = 0 });
            await _cScalcPacketHandler!.ExecuteAsync(new CScalcPacket
            {
                BazaarId = 0,
                Price = 50,
                Amount = 1,
                VNum = 1012
            }, _session!).ConfigureAwait(false);
            var lastpacket = (RCScalcPacket?) _session!.LastPackets.FirstOrDefault(s => s is RCScalcPacket);
            Assert.AreEqual(50, lastpacket?.Total);
        }
    }
}