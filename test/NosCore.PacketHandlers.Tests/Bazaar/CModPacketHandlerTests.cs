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
using Json.Patch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Bazaar
{
    [TestClass]
    public class CModPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Mock<IBazaarHub> BazaarHttpClient = null!;
        private CModPacketHandler CmodPacketHandler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            BazaarHttpClient = new Mock<IBazaarHub>();
            CmodPacketHandler = new CModPacketHandler(BazaarHttpClient.Object, Logger, TestHelpers.Instance.LogLanguageLocalizer);

            BazaarHttpClient.Setup(b => b.GetBazaar(0, null, null, null, null, null, null, null, null)).ReturnsAsync(
                new List<BazaarLink>() {new()
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto { Price = 50, Amount = 1 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 1 }
                }});

            BazaarHttpClient.Setup(b => b.GetBazaar(3, null, null, null, null, null, null, null, null)).ReturnsAsync(
                new List<BazaarLink>() {new()
                {
                    SellerName = Session.Character.Name,
                    BazaarItem = new BazaarItemDto { Price = 50, Amount = 1 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 0 }
                }});

            BazaarHttpClient.Setup(b => b.GetBazaar(2, null, null, null, null, null, null, null, null)).ReturnsAsync(
                new List<BazaarLink>() {new()
                {
                    SellerName = Session.Character.Name,
                    BazaarItem = new BazaarItemDto { Price = 60, Amount = 1 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 1 }
                }});
            BazaarHttpClient.Setup(b => b.GetBazaar(1, null, null, null, null, null, null, null, null)).ReturnsAsync(new List<BazaarLink>());
            BazaarHttpClient.Setup(b => b.ModifyBazaarAsync(It.IsAny<long>(), It.IsAny<JsonPatch?>()!)).ReturnsAsync(new BazaarLink
            {
                SellerName = Session.Character.Name,
                BazaarItem = new BazaarItemDto { Price = 70, Amount = 1 },
                ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 1 }
            });
        }

        [TestMethod]
        public async Task ModifyingWhileInShopShouldBeIgnored()
        {
            await new Spec("Modifying while in shop should be ignored")
                .Given(CharacterIsInShop)
                .WhenAsync(ModifyingBazaarItemAsync)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ModifyingNonExistentItemShouldBeIgnored()
        {
            await new Spec("Modifying non existent item should be ignored")
                .WhenAsync(ModifyingNonExistentItemAsync)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ModifyingOtherSellersItemShouldBeIgnored()
        {
            await new Spec("Modifying other sellers item should be ignored")
                .WhenAsync(ModifyingOtherSellersItemAsync)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ModifyingSoldItemShouldShowError()
        {
            await new Spec("Modifying sold item should show error")
                .WhenAsync(ModifyingSoldItemAsync)
                .Then(ShouldReceiveCannotChangePriceMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ModifyingWithWrongAmountShouldShowOfferUpdated()
        {
            await new Spec("Modifying with wrong amount should show offer updated")
                .WhenAsync(ModifyingWithWrongAmountAsync)
                .Then(ShouldReceiveOfferUpdatedMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ModifyingToSamePriceShouldBeIgnored()
        {
            await new Spec("Modifying to same price should be ignored")
                .WhenAsync(ModifyingToSamePriceAsync)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ModifyingPriceShouldSucceed()
        {
            await new Spec("Modifying price should succeed")
                .WhenAsync(ModifyingPriceSuccessfullyAsync)
                .Then(ShouldReceiveNewPriceMessage)
                .ExecuteAsync();
        }

        private void CharacterIsInShop()
        {
            Session.Character.InShop = true;
        }

        private async Task ModifyingBazaarItemAsync()
        {
            await CmodPacketHandler.ExecuteAsync(new CModPacket
            {
                BazaarId = 1, NewPrice = 50, Amount = 1, VNum = 1012
            }, Session);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.IsNull(Session.LastPackets.FirstOrDefault());
        }

        private async Task ModifyingNonExistentItemAsync()
        {
            await CmodPacketHandler.ExecuteAsync(new CModPacket
            {
                BazaarId = 1, NewPrice = 50, Amount = 1, VNum = 1012
            }, Session);
        }

        private async Task ModifyingOtherSellersItemAsync()
        {
            await CmodPacketHandler.ExecuteAsync(new CModPacket
            {
                BazaarId = 0, NewPrice = 50, Amount = 1, VNum = 1012
            }, Session);
        }

        private async Task ModifyingSoldItemAsync()
        {
            await CmodPacketHandler.ExecuteAsync(new CModPacket
            {
                BazaarId = 3, NewPrice = 60, Amount = 1, VNum = 1012
            }, Session);
        }

        private void ShouldReceiveCannotChangePriceMessage()
        {
            var packet = (ModaliPacket?)Session.LastPackets.FirstOrDefault(s => s is ModaliPacket);
            Assert.IsTrue(packet?.Type == 1 && packet?.Message == Game18NConstString.CannotChangePriceSoldItems);
        }

        private async Task ModifyingWithWrongAmountAsync()
        {
            await CmodPacketHandler.ExecuteAsync(new CModPacket
            {
                BazaarId = 2, NewPrice = 70, Amount = 2, VNum = 1012
            }, Session);
        }

        private void ShouldReceiveOfferUpdatedMessage()
        {
            var packet = (ModaliPacket?)Session.LastPackets.FirstOrDefault(s => s is ModaliPacket);
            Assert.IsTrue(packet?.Type == 1 && packet?.Message == Game18NConstString.OfferUpdated);
        }

        private async Task ModifyingToSamePriceAsync()
        {
            await CmodPacketHandler.ExecuteAsync(new CModPacket
            {
                BazaarId = 2, NewPrice = 60, Amount = 1, VNum = 1012
            }, Session);
        }

        private async Task ModifyingPriceSuccessfullyAsync()
        {
            await CmodPacketHandler.ExecuteAsync(new CModPacket
            {
                BazaarId = 2, NewPrice = 70, Amount = 1, VNum = 1012
            }, Session);
        }

        private void ShouldReceiveNewPriceMessage()
        {
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player &&
                packet?.VisualId == Session.Character.CharacterId &&
                packet?.Type == SayColorType.Yellow &&
                packet?.Message == Game18NConstString.NewSellingPrice &&
                packet?.ArgumentType == 4 && (long?)packet?.Game18NArguments[0] == 70);
        }
    }
}
