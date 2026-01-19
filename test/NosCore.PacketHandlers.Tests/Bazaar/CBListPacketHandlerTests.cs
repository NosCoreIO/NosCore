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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Auction;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Bazaar
{
    [TestClass]
    public class CBListPacketHandlerTests
    {
        private Mock<IBazaarHub> BazaarHttpClient = null!;
        private CBListPacketHandler CblistPacketHandler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            BazaarHttpClient = new Mock<IBazaarHub>();
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Main, VNum = 1012, IsSoldable = true}
            };
            CblistPacketHandler = new CBListPacketHandler(BazaarHttpClient.Object, items, TestHelpers.Instance.Clock);
        }

        [TestMethod]
        public async Task ListingShouldReturnEmptyWhenNoItems()
        {
            await new Spec("Listing should return empty when no items")
                .Given(BazaarHasNoItems)
                .WhenAsync(ListingBazaarItems)
                .Then(ShouldReceiveEmptyList)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ListingShouldReturnValidItems()
        {
            await new Spec("Listing should return valid items")
                .Given(BazaarHasValidItems)
                .WhenAsync(ListingBazaarItems)
                .Then(ShouldReceiveOneItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ListingShouldFilterInvalidItems()
        {
            await new Spec("Listing should filter invalid items")
                .Given(BazaarHasInvalidItems)
                .WhenAsync(ListingBazaarItems)
                .Then(ShouldReceiveEmptyList)
                .ExecuteAsync();
        }

        private void BazaarHasNoItems()
        {
            BazaarHttpClient.Setup(b =>
                b.GetBazaar(
                    It.IsAny<long>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<BazaarListType?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<long?>())
            ).ReturnsAsync(new List<BazaarLink>());
        }

        private async Task ListingBazaarItems()
        {
            await CblistPacketHandler.ExecuteAsync(new CBListPacket { ItemVNumFilter = new List<short>() }, Session);
        }

        private void ShouldReceiveEmptyList()
        {
            var packet = (RcbListPacket?)Session.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(packet?.Items?.Count == 0);
        }

        private void BazaarHasValidItems()
        {
            BazaarHttpClient.Setup(b =>
                b.GetBazaar(
                    It.IsAny<long>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<BazaarListType?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<long?>())
            ).ReturnsAsync(new List<BazaarLink>
            {
                new()
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto
                        {Price = 50, Amount = 1, DateStart = TestHelpers.Instance.Clock.GetCurrentInstant(), Duration = 200},
                    ItemInstance = new ItemInstanceDto {ItemVNum = 1012, Amount = 1}
                }
            });
        }

        private void ShouldReceiveOneItem()
        {
            var packet = (RcbListPacket?)Session.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(packet?.Items?.Count == 1);
        }

        private void BazaarHasInvalidItems()
        {
            BazaarHttpClient.Setup(b =>
                b.GetBazaar(
                    It.IsAny<long>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<BazaarListType?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<byte?>(),
                    It.IsAny<long?>())
            ).ReturnsAsync(new List<BazaarLink>
            {
                new()
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto {Price = 50, Amount = 1},
                    ItemInstance = new ItemInstanceDto {ItemVNum = 1012, Amount = 1}
                }
            });
        }
    }
}
