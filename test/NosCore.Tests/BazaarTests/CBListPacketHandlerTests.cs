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
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Auction;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CbListPacketHandlerTest
    {
        private Mock<IBazaarHttpClient>? _bazaarHttpClient;
        private CBListPacketHandler? _cblistPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            Broadcaster.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _bazaarHttpClient = new Mock<IBazaarHttpClient>();
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Main, VNum = 1012, IsSoldable = true}
            };
            _cblistPacketHandler = new CBListPacketHandler(_bazaarHttpClient.Object, items);
        }

        [TestMethod]
        public async Task ListShouldReturnEmptyWhenNoItems()
        {
            _bazaarHttpClient!.Setup(b =>
                b.GetBazaarLinks(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<BazaarListType>(),
                    It.IsAny<byte>(),
                    It.IsAny<byte>(),
                    It.IsAny<byte>(),
                    It.IsAny<byte>(),
                    It.IsAny<long?>())
            ).ReturnsAsync(new List<BazaarLink>());
            await _cblistPacketHandler!.Execute(new CBListPacket {ItemVNumFilter = new List<short>()}, _session!).ConfigureAwait(false);
            var lastpacket = (RcbListPacket?) _session!.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(lastpacket?.Items?.Count == 0);
        }

        [TestMethod]
        public async Task ListShouldReturnTheItems()
        {
            _bazaarHttpClient!.Setup(b =>
                b.GetBazaarLinks(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<BazaarListType>(),
                    It.IsAny<byte>(),
                    It.IsAny<byte>(),
                    It.IsAny<byte>(),
                    It.IsAny<byte>(),
                    It.IsAny<long?>())
            ).ReturnsAsync(new List<BazaarLink>
            {
                new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto
                        {Price = 50, Amount = 1, DateStart = SystemTime.Now(), Duration = 200},
                    ItemInstance = new ItemInstanceDto {ItemVNum = 1012, Amount = 1}
                }
            });
            await _cblistPacketHandler!.Execute(new CBListPacket {ItemVNumFilter = new List<short>()}, _session!).ConfigureAwait(false);
            var lastpacket = (RcbListPacket?) _session!.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(lastpacket?.Items?.Count == 1);
        }

        [TestMethod]
        public async Task ListShouldReturnTheItemsNotValid()
        {
            _bazaarHttpClient!.Setup(b =>
                b.GetBazaarLinks(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<BazaarListType>(),
                    It.IsAny<byte>(),
                    It.IsAny<byte>(),
                    It.IsAny<byte>(),
                    It.IsAny<byte>(),
                    It.IsAny<long?>())
            ).ReturnsAsync(new List<BazaarLink>
            {
                new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto {Price = 50, Amount = 1},
                    ItemInstance = new ItemInstanceDto {ItemVNum = 1012, Amount = 1}
                }
            });
            await _cblistPacketHandler!.Execute(new CBListPacket {ItemVNumFilter = new List<short>()}, _session!).ConfigureAwait(false);
            var lastpacket = (RcbListPacket?) _session!.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(lastpacket?.Items?.Count == 0);
        }

        //todo list filter
    }
}