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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Auction;
using NosCore.Tests.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CbListPacketHandlerTest
    {
        private Mock<IBazaarHttpClient>? _bazaarHttpClient;
        private CBListPacketHandler? _cblistPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            Broadcaster.Reset();
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _bazaarHttpClient = new Mock<IBazaarHttpClient>();
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Main, VNum = 1012, IsSoldable = true}
            };
            _cblistPacketHandler = new CBListPacketHandler(_bazaarHttpClient.Object, items);
        }

        [TestMethod]
        public async Task ListShouldReturnEmptyWhenNoItemsAsync()
        {
            _bazaarHttpClient!.Setup(b =>
                b.GetBazaarLinksAsync(
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
            await _cblistPacketHandler!.ExecuteAsync(new CBListPacket { ItemVNumFilter = new List<short>() }, _session!).ConfigureAwait(false);
            var lastpacket = (RcbListPacket?)_session!.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(lastpacket?.Items?.Count == 0);
        }

        [TestMethod]
        public async Task ListShouldReturnTheItemsAsync()
        {
            _bazaarHttpClient!.Setup(b =>
                b.GetBazaarLinksAsync(
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
            await _cblistPacketHandler!.ExecuteAsync(new CBListPacket { ItemVNumFilter = new List<short>() }, _session!).ConfigureAwait(false);
            var lastpacket = (RcbListPacket?)_session!.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(lastpacket?.Items?.Count == 1);
        }

        [TestMethod]
        public async Task ListShouldReturnTheItemsNotValidAsync()
        {
            _bazaarHttpClient!.Setup(b =>
                b.GetBazaarLinksAsync(
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
            await _cblistPacketHandler!.ExecuteAsync(new CBListPacket { ItemVNumFilter = new List<short>() }, _session!).ConfigureAwait(false);
            var lastpacket = (RcbListPacket?)_session!.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(lastpacket?.Items?.Count == 0);
        }

        //todo list filter
    }
}