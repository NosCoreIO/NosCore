//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Text;
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Handling;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ExchangeInfo;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Map;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class ExchangePacketControllerTests
    {
        private ClientSession _session;
        private ClientSession _session2;
        private Character _character;
        private Character _character2;
        private ExchangePacketController _controller;
        private ExchangePacketController _controller2;
        private WorldConfiguration _worldConfiguration;
        private readonly ExchangeAccessService _exchangeAccessService = new ExchangeAccessService();

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            Broadcaster.Reset();

            var account = new AccountDto { Name = "AccountTest", Password = "test".ToSha512() };
            var account2 = new AccountDto { Name = "AccountTest2", Password = "test".ToSha512() };

            _character = new Character
            {
                CharacterId = 1,
                Name = "Test",
                Slot = 1,
                AccountId = 1,
                MapId = 1,
                State = CharacterState.Active
            };

            _character2 = new Character
            {
                CharacterId = 2,
                Name = "Test2",
                Slot = 1,
                AccountId = 2,
                MapId = 1,
                State = CharacterState.Active
            };
            var channelMock = new Mock<IChannel>();

            _session = new ClientSession(null, new List<PacketController> { new ExchangePacketController(_worldConfiguration, _exchangeAccessService) });
            _session.RegisterChannel(channelMock.Object);
            _session.InitializeAccount(account);
            _session.SessionId = 1;

            _controller = new ExchangePacketController(_worldConfiguration, _exchangeAccessService);
            _controller.RegisterSession(_session);
            _session.SetCharacter(_character);
            _exchangeAccessService.ExchangeDatas[_session.Character.CharacterId] = new ExchangeData();
            Broadcaster.Instance.RegisterSession(_session);

            _session2 = new ClientSession(null, new List<PacketController> { new ExchangePacketController(_worldConfiguration, _exchangeAccessService) });
            _session2.RegisterChannel(channelMock.Object);
            _session2.InitializeAccount(account2);
            _session2.SessionId = 2;

            _controller2 = new ExchangePacketController(_worldConfiguration, _exchangeAccessService);
            _controller2.RegisterSession(_session2);
            _session2.SetCharacter(_character2);
            _exchangeAccessService.ExchangeDatas[_session2.Character.CharacterId] = new ExchangeData();
            Broadcaster.Instance.RegisterSession(_session2);
        }

        [TestMethod]
        public void Test_Accept_Request()
        {
            var packet = new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.List,
                VisualId = _character2.CharacterId
            };

            _controller.RequestExchange(packet);
            Assert.IsTrue(_exchangeAccessService.ExchangeDatas[_session.Character.CharacterId].TargetVisualId == _character2.CharacterId && _character2.InExchangeOrShop);
        }

        [TestMethod]
        public void Test_Cancel_Request()
        {
            _exchangeAccessService.ExchangeDatas[_session.Character.CharacterId].TargetVisualId = _character2.VisualId;
            _character.InExchange = true;
            _character2.InExchange = true;

            var packet = new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.Cancelled,
                VisualId = _character2.VisualId
            };

            _controller.RequestExchange(packet);
            Assert.IsTrue(!_character.InExchangeOrShop && !_character2.InExchangeOrShop);
        }
    }
}
