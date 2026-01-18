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
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Group;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Tests.Shared;
using Serilog;

namespace NosCore.PacketHandlers.Tests.Group
{
    [TestClass]
    public class PJoinPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private readonly Dictionary<int, ClientSession> _sessions = new();
        private PjoinPacketHandler? _pJoinPacketHandler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            var idServer = new IdService<GameObject.Group>(1);
            for (byte i = 0; i < (byte)(GroupType.Group + 1); i++)
            {
                var session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
                var mockChannel = new Mock<IChannel>();
                mockChannel.Setup(s => s.Id).Returns(Guid.NewGuid().ToString());
                session.RegisterChannel(mockChannel.Object);
                _sessions.Add(i, session);
                var sessionGroupFactoryMock = new Mock<ISessionGroupFactory>();
                sessionGroupFactoryMock.Setup(x => x.Create()).Returns(new Mock<ISessionGroup>().Object);
                session.Character.Group = new GameObject.Group(GroupType.Group, sessionGroupFactoryMock.Object);
                session.Character.Group.JoinGroup(session.Character);
            }

            var mock = new Mock<IBlacklistHub>();
            _pJoinPacketHandler = new PjoinPacketHandler(Logger, mock.Object, TestHelpers.Instance.Clock, idServer, TestHelpers.Instance.LogLanguageLocalizer, TestHelpers.Instance.GameLanguageLocalizer, TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task Test_Accept_Group_Join_RequestedAsync()
        {
            _sessions[1].Character.GroupRequestCharacterIds
                .TryAdd(_sessions[0].Character.CharacterId, _sessions[0].Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _sessions[1].Character.CharacterId
            };

            await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _sessions[0]).ConfigureAwait(false);
            Assert.IsTrue((_sessions[0].Character.Group!.Count > 1)
                && (_sessions[1].Character.Group!.Count > 1)
                && (_sessions[0].Character.Group!.GroupId
                    == _sessions[1].Character.Group!.GroupId));
        }

        [TestMethod]
        public async Task Test_Join_Full_GroupAsync()
        {
            PjoinPacket pjoinPacket;

            for (var i = 1; i < 3; i++)
            {
                _sessions[i].Character.GroupRequestCharacterIds
                    .TryAdd(_sessions[0].Character.CharacterId, _sessions[0].Character.CharacterId);

                pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = _sessions[i].Character.CharacterId
                };

                await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _sessions[0]).ConfigureAwait(false);
            }

            Assert.IsTrue(_sessions[0].Character.Group!.IsGroupFull
                && _sessions[1].Character.Group!.IsGroupFull
                && _sessions[2].Character.Group!.IsGroupFull);

            _sessions[3].Character.GroupRequestCharacterIds
                .TryAdd(_sessions[0].Character.CharacterId, _sessions[0].Character.CharacterId);

            pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _sessions[3].Character.CharacterId
            };

            await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _sessions[0]).ConfigureAwait(false);
            Assert.IsTrue(_sessions[3].Character.Group!.Count == 1);
        }

        [TestMethod]
        public async Task Test_Accept_Not_Requested_GroupAsync()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _sessions[1].Character.CharacterId
            };

            await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _sessions[0]).ConfigureAwait(false);
            Assert.IsTrue((_sessions[0].Character.Group!.Count == 1)
                && (_sessions[1].Character.Group!.Count == 1));
        }

        [TestMethod]
        public async Task Test_Decline_Not_Requested_GroupAsync()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Declined,
                CharacterId = _sessions[1].Character.CharacterId
            };

            await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _sessions[0]).ConfigureAwait(false);
            Assert.IsTrue((_sessions[0].Character.Group!.Count == 1)
                && (_sessions[1].Character.Group!.Count == 1));
        }

        [TestMethod]
        public async Task Test_Last_Request_Not_Null_After_OneAsync()
        {
            for (var i = 1; i < 3; i++)
            {
                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Invited,
                    CharacterId = _sessions[i].Character.CharacterId
                };

                await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _sessions[0]).ConfigureAwait(false);
            }
            Assert.IsNotNull(_sessions[0].Character.LastGroupRequest);
        }

        [TestMethod]
        public async Task Test_Two_Request_Less_5_Sec_DelayAsync()
        {
            for (var i = 1; i < 3; i++)
            {
                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Invited,
                    CharacterId = _sessions[i].Character.CharacterId
                };
                TestHelpers.Instance.Clock.AdvanceSeconds(1);
                await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _sessions[0]).ConfigureAwait(false);
            }

            Assert.IsTrue(_sessions[0].Character.GroupRequestCharacterIds.Count == 1);
        }

        [TestMethod]
        public async Task Test_Two_Request_More_5_Sec_DelayAsync()
        {
            for (var i = 1; i < 3; i++)
            {
                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Invited,
                    CharacterId = _sessions[i].Character.CharacterId
                };

                if (i == 2)
                {
                    TestHelpers.Instance.Clock.AdvanceMinutes(6);
                }

                await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _sessions[0]).ConfigureAwait(false);
            }

            Assert.IsTrue(_sessions[0].Character.GroupRequestCharacterIds.Count == 2);
        }
    }
}