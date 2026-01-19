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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Parcel;
using NosCore.Packets.ClientPackets.Parcel;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Parcel
{
    [TestClass]
    public class PclPacketHandlerTests
    {
        private Mock<IMailHub> MailHttpClient = null!;
        private PclPacketHandler PclPacketHandler = null!;
        private IItemGenerationService Item = null!;
        private ClientSession Session = null!;
        private Mock<IDao<IItemInstanceDto?, Guid>> ItemInstanceDao = null!;
        private MailData? Mail;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Item = TestHelpers.Instance.GenerateItemProvider();
            MailHttpClient = new Mock<IMailHub>();
            ItemInstanceDao = new Mock<IDao<IItemInstanceDto?, Guid>>();
            PclPacketHandler = new PclPacketHandler(MailHttpClient.Object, Item, ItemInstanceDao.Object);
        }

        [TestMethod]
        public async Task GiftNotFoundShouldReturnNoPacket()
        {
            await new Spec("Gift not found should return no packet")
                .Given(NoGiftExists)
                .WhenAsync(DeletingGift)
                .Then(NoParcelPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingGiftShouldReturnDeletePacket()
        {
            await new Spec("Deleting gift should return delete packet")
                .Given(GiftExists)
                .WhenAsync(DeletingGift)
                .Then(DeleteParcelPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReceivingGiftShouldReturnReceivePacket()
        {
            await new Spec("Receiving gift should return receive packet")
                .Given(GiftExists)
                .WhenAsync(ReceivingGift)
                .Then(ReceiveParcelPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReceivingGiftWithNoSpaceShouldReturnNoSpacePacket()
        {
            await new Spec("Receiving gift with no space should return no space packet")
                .Given(GiftExists)
                .And(InventoryIsFull)
                .WhenAsync(ReceivingGift)
                .Then(NoSpaceParcelPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void NoGiftExists()
        {
            MailHttpClient.Setup(s => s.GetMails(1, Session.Character.CharacterId, false)).ReturnsAsync(new List<MailData>());
        }

        private void GiftExists()
        {
            var item = Item.Create(1);
            Mail = new MailData
            {
                ItemInstance = (ItemInstanceDto)item,
                MailDto = new MailDto
                {
                    ItemInstanceId = item.Id
                }
            };
            ItemInstanceDao.Setup(o => o.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(item);
            MailHttpClient.Setup(s => s.GetMails(1, Session.Character.CharacterId, false)).ReturnsAsync(new List<MailData> { Mail });
        }

        private void InventoryIsFull()
        {
            TestHelpers.Instance.WorldConfiguration.Value.BackpackSize = 0;
        }

        private async Task DeletingGift()
        {
            await PclPacketHandler.ExecuteAsync(new PclPacket
            {
                Type = 5,
                GiftId = 1
            }, Session);
        }

        private async Task ReceivingGift()
        {
            await PclPacketHandler.ExecuteAsync(new PclPacket
            {
                Type = 4,
                GiftId = 1
            }, Session);
        }

        private void NoParcelPacketShouldBeSent()
        {
            var packet = (ParcelPacket?)Session.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsNull(packet);
        }

        private void DeleteParcelPacketShouldBeSent()
        {
            var packet = (ParcelPacket?)Session.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsTrue(packet?.Type == 7);
        }

        private void ReceiveParcelPacketShouldBeSent()
        {
            var packet = (ParcelPacket?)Session.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsTrue(packet?.Type == 2);
        }

        private void NoSpaceParcelPacketShouldBeSent()
        {
            var packet = (ParcelPacket?)Session.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsTrue(packet?.Type == 5);
        }
    }
}
