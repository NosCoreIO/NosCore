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

using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CModPacketHandlerTest
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Mock<IBazaarHttpClient>? _bazaarHttpClient;
        private CModPacketHandler? _cmodPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            Broadcaster.Reset();
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _bazaarHttpClient = new Mock<IBazaarHttpClient>();
            _cmodPacketHandler = new CModPacketHandler(_bazaarHttpClient.Object, Logger);

            _bazaarHttpClient.Setup(b => b.GetBazaarLinkAsync(0)).ReturnsAsync(
                new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto {Price = 50, Amount = 1},
                    ItemInstance = new ItemInstanceDto {ItemVNum = 1012, Amount = 1}
                });

            _bazaarHttpClient.Setup(b => b.GetBazaarLinkAsync(3)).ReturnsAsync(
                new BazaarLink
                {
                    SellerName = _session.Character.Name,
                    BazaarItem = new BazaarItemDto {Price = 50, Amount = 1},
                    ItemInstance = new ItemInstanceDto {ItemVNum = 1012, Amount = 0}
                });

            _bazaarHttpClient.Setup(b => b.GetBazaarLinkAsync(2)).ReturnsAsync(
                new BazaarLink
                {
                    SellerName = _session.Character.Name,
                    BazaarItem = new BazaarItemDto {Price = 60, Amount = 1},
                    ItemInstance = new ItemInstanceDto {ItemVNum = 1012, Amount = 1}
                });
            _bazaarHttpClient.Setup(b => b.GetBazaarLinkAsync(1)).ReturnsAsync((BazaarLink?) null);
            _bazaarHttpClient.Setup(b => b.ModifyAsync(It.IsAny<long>(), It.IsAny<JsonPatchDocument<BazaarLink>>())).ReturnsAsync(new BazaarLink
                {
                    SellerName = _session.Character.Name,
                    BazaarItem = new BazaarItemDto {Price = 70, Amount = 1},
                    ItemInstance = new ItemInstanceDto {ItemVNum = 1012, Amount = 1}
                });
        }

        [TestMethod]
        public async Task ModifyWhenInExchangeAsync()
        {
            _session!.Character.InShop = true;
            await _cmodPacketHandler!.ExecuteAsync(new CModPacket
            {
                BazaarId = 1,
                NewPrice = 50,
                Amount = 1,
                VNum = 1012
            }, _session).ConfigureAwait(false);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public async Task ModifyWhenNoItemAsync()
        {
            await _cmodPacketHandler!.ExecuteAsync(new CModPacket
            {
                BazaarId = 1,
                NewPrice = 50,
                Amount = 1,
                VNum = 1012
            }, _session!).ConfigureAwait(false);
            Assert.IsNull(_session!.LastPackets.FirstOrDefault());
        }


        [TestMethod]
        public async Task ModifyWhenOtherSellerAsync()
        {
            await _cmodPacketHandler!.ExecuteAsync(new CModPacket
            {
                BazaarId = 0,
                NewPrice = 50,
                Amount = 1,
                VNum = 1012
            }, _session!).ConfigureAwait(false);
            Assert.IsNull(_session!.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public async Task ModifyWhenSoldAsync()
        {
           await _cmodPacketHandler!.ExecuteAsync(new CModPacket
           {
               BazaarId = 3,
               NewPrice = 60,
               Amount = 1,
               VNum = 1012
           }, _session!).ConfigureAwait(false);
            var lastpacket = (ModalPacket?) _session!.LastPackets.FirstOrDefault(s => s is ModalPacket);
            Assert.IsTrue(lastpacket?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.CAN_NOT_MODIFY_SOLD_ITEMS, _session.Account.Language));
        }

        [TestMethod]
        public async Task ModifyWhenWrongAmountAsync()
        {
            await _cmodPacketHandler!.ExecuteAsync(new CModPacket
            {
                BazaarId = 2,
                NewPrice = 70,
                Amount = 2,
                VNum = 1012
            }, _session!).ConfigureAwait(false);
            var lastpacket = (ModalPacket?) _session!.LastPackets.FirstOrDefault(s => s is ModalPacket);
            Assert.IsTrue(lastpacket?.Message ==
                GameLanguage.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, _session.Account.Language));
        }

        [TestMethod]
        public async Task ModifyWhenPriceSamePriceAsync()
        {
           await _cmodPacketHandler!.ExecuteAsync(new CModPacket
           {
               BazaarId = 2,
               NewPrice = 60,
               Amount = 1,
               VNum = 1012
           }, _session!).ConfigureAwait(false);
            Assert.IsNull(_session!.LastPackets.FirstOrDefault());
        }

        [TestMethod]
        public async Task ModifyAsync()
        {
            await _cmodPacketHandler!.ExecuteAsync(new CModPacket
            {
                BazaarId = 2,
                NewPrice = 70,
                Amount = 1,
                VNum = 1012
            }, _session!).ConfigureAwait(false);
            var lastpacket = (SayPacket?) _session!.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue(lastpacket?.Message ==
                string.Format(
                    GameLanguage.Instance.GetMessageFromKey(LanguageKey.BAZAAR_PRICE_CHANGED, _session.Account.Language),
                    70
                ));
        }
    }
}