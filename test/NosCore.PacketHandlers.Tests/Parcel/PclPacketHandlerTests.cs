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

namespace NosCore.PacketHandlers.Tests.Parcel
{
    [TestClass]
    public class PclPacketHandlerTests
    {
        private Mock<IMailHub>? _mailHttpClient;
        private PclPacketHandler? _pclPacketHandler;
        private IItemGenerationService? _item;
        private ClientSession? _session;
        private Mock<IDao<IItemInstanceDto?, Guid>>? _itemInstanceDao;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _item = TestHelpers.Instance.GenerateItemProvider();
            _mailHttpClient = new Mock<IMailHub>();
            _itemInstanceDao = new Mock<IDao<IItemInstanceDto?, Guid>>();
            _pclPacketHandler = new PclPacketHandler(_mailHttpClient.Object, _item, _itemInstanceDao.Object);
        }

        [TestMethod]
        public async Task Test_GiftNotFoundAsync()
        {
            _mailHttpClient!.Setup(s => s.GetMails(1, _session!.Character.CharacterId, false)).ReturnsAsync(new List<MailData>());
            await _pclPacketHandler!.ExecuteAsync(new PclPacket
            {
                Type = 5,
                GiftId = 1
            }, _session!);
            var packet = (ParcelPacket?)_session!.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public async Task Test_DeleteGiftAsync()
        {
            var item = _item!.Create(1);
            var mail = new MailData
            {
                ItemInstance = (ItemInstanceDto)item,
                MailDto = new MailDto
                {
                    ItemInstanceId = item.Id
                }
            };
            _itemInstanceDao!.Setup(o => o.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(item);
            _mailHttpClient!.Setup(s => s.GetMails(1, _session!.Character.CharacterId, false)).ReturnsAsync(new List<MailData>() { mail });
            await _pclPacketHandler!.ExecuteAsync(new PclPacket
            {
                Type = 5,
                GiftId = 1
            }, _session!);
            var packet = (ParcelPacket?)_session!.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsTrue(packet?.Type == 7);
        }

        [TestMethod]
        public async Task Test_ReceiveGiftAsync()
        {
            var item = _item!.Create(1);
            var mail = new MailData
            {
                ItemInstance = (ItemInstanceDto)item,
                MailDto = new MailDto
                {
                    ItemInstanceId = item.Id
                }
            };
            _itemInstanceDao!.Setup(o => o.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(item);
            _mailHttpClient!.Setup(s => s.GetMails(1, _session!.Character.CharacterId, false)).ReturnsAsync(new List<MailData> { mail });
            await _pclPacketHandler!.ExecuteAsync(new PclPacket
            {
                Type = 4,
                GiftId = 1
            }, _session!);
            var packet = (ParcelPacket?)_session!.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsTrue(packet?.Type == 2);
        }

        [TestMethod]
        public async Task Test_ReceiveGiftNoPlaceAsync()
        {
            TestHelpers.Instance.WorldConfiguration.Value.BackpackSize = 0;
            var item = _item!.Create(1);
            var mail = new MailData
            {
                ItemInstance = (ItemInstanceDto)item,
                MailDto = new MailDto
                {
                    ItemInstanceId = item.Id
                }
            };
            _itemInstanceDao!.Setup(o => o.FirstOrDefaultAsync(It.IsAny<Expression<Func<IItemInstanceDto?, bool>>>()))
                .ReturnsAsync(item);
            _mailHttpClient!.Setup(s => s.GetMails(1, _session!.Character.CharacterId, false)).ReturnsAsync(new List<MailData> { mail });

            await _pclPacketHandler!.ExecuteAsync(new PclPacket
            {
                Type = 4,
                GiftId = 1
            }, _session!);
            var packet = (ParcelPacket?)_session!.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsTrue(packet?.Type == 5);
        }
    }
}