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
using NosCore.Packets.ClientPackets.Parcel;
using NosCore.Packets.ServerPackets.Parcel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.HttpClients.MailHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.PacketHandlers.Parcel;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.ParcelTests
{
    [TestClass]
    public class PclPacketHandlerTests
    {
        private Mock<IMailHttpClient> _mailHttpClient;
        private PclPacketHandler _pclPacketHandler;
        private IItemProvider _item;
        private ClientSession _session;
        private Mock<IGenericDao<IItemInstanceDto>> _itemInstanceDao;

        [TestInitialize]
        public void Setup()
        {
            SystemTime.Freeze();
            _session = TestHelpers.Instance.GenerateSession();
            _item = TestHelpers.Instance.GenerateItemProvider();
            _mailHttpClient = new Mock<IMailHttpClient>();
            _itemInstanceDao = new Mock<IGenericDao<IItemInstanceDto>>();
            _pclPacketHandler = new PclPacketHandler(_mailHttpClient.Object, _item, _itemInstanceDao.Object);
        }

        [TestMethod]
        public async Task Test_GiftNotFound()
        {
            _mailHttpClient.Setup(s => s.GetGift(1, _session.Character.CharacterId, false)).ReturnsAsync((MailData)null);
            await _pclPacketHandler.Execute(new PclPacket
            {
                Type = 5,
                GiftId = 1
            }, _session);
            var packet = (ParcelPacket?)_session.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsNull(packet);
        }

        [TestMethod]
        public async Task Test_DeleteGift()
        {
            _mailHttpClient.Setup(s => s.GetGift(1, _session.Character.CharacterId, false)).ReturnsAsync(new MailData());
            await _pclPacketHandler.Execute(new PclPacket
            {
                Type = 5,
                GiftId = 1
            }, _session);
            var packet = (ParcelPacket?)_session.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsTrue(packet.Type == 7);
        }

        [TestMethod]
        public async Task Test_ReceiveGift()
        {
            var item = _item.Create(1);
            var mail = new MailData
            {
                ItemInstance = (ItemInstanceDto) item,
                MailDto = new MailDto
                {
                    ItemInstanceId = item.Id
                }
            };
            _itemInstanceDao.Setup(o => o.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(item);
            _mailHttpClient.Setup(s => s.GetGift(1, _session.Character.CharacterId, false)).ReturnsAsync(mail);
            await _pclPacketHandler.Execute(new PclPacket
            {
                Type = 4,
                GiftId = 1
            }, _session);
            var packet = (ParcelPacket?)_session.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsTrue(packet?.Type == 2);
        }

        [TestMethod]
        public async Task Test_ReceiveGiftNoPlace()
        {
            TestHelpers.Instance.WorldConfiguration.BackpackSize = 0;
            var item = _item.Create(1);
            var mail = new MailData
            {
                ItemInstance = (ItemInstanceDto)item,
                MailDto = new MailDto
                {
                    ItemInstanceId = item.Id
                }
            };
            _itemInstanceDao.Setup(o => o.FirstOrDefault(It.IsAny<Expression<Func<IItemInstanceDto, bool>>>()))
                .Returns(item);
            _mailHttpClient.Setup(s => s.GetGift(1, _session.Character.CharacterId, false)).ReturnsAsync(mail);
            await _pclPacketHandler.Execute(new PclPacket
            {
                Type = 4,
                GiftId = 1
            }, _session);
            var packet = (ParcelPacket?)_session.LastPackets.FirstOrDefault(s => s is ParcelPacket);
            Assert.IsTrue(packet.Type == 5);
        }
    }
}