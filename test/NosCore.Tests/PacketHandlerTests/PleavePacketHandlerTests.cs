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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Groups;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Groups;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.Group;
using NosCore.PacketHandlers.Group;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class PleavePacketHandlerTests
    {
        private static readonly ILogger Logger = Core.I18N.Logger.GetLoggerConfiguration().CreateLogger();
        private readonly Dictionary<int, Character> _characters = new Dictionary<int, Character>();
        private PjoinPacketHandler? _pJoinPacketHandler;
        private PleavePacketHandler? _pLeavePacketHandler;

        [TestInitialize]
        public void Setup()
        {
            Broadcaster.Reset();
            GroupAccess.Instance.Groups = new ConcurrentDictionary<long, Group>();
            for (byte i = 0; i < (byte) (GroupType.Group + 1); i++)
            {
                var session = TestHelpers.Instance.GenerateSession();
                session.RegisterChannel(null);
                _characters.Add(i, session.Character!);
                session.Character.Group.JoinGroup(session.Character);
            }

            _pLeavePacketHandler = new PleavePacketHandler();

            var mock = new Mock<IBlacklistHttpClient>();
            _pJoinPacketHandler = new PjoinPacketHandler(Logger, mock.Object);
        }

        [TestMethod]
        public async Task Test_Leave_Group_When_Not_Grouped()
        {
            await _pLeavePacketHandler!.Execute(new PleavePacket(), _characters[0].Session);

            Assert.IsTrue((_characters[0].Group != null) && (_characters[0].Group.Count == 1));
        }

        [TestMethod]
        public async Task Test_Leave_Group_When_Grouped()
        {
            for (var i = 1; i < 3; i++)
            {
                _characters[i].GroupRequestCharacterIds
                    .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = _characters[i].CharacterId
                };

                await _pJoinPacketHandler!.Execute(pjoinPacket, _characters[0].Session);
            }

            Assert.IsTrue(_characters[0].Group.IsGroupFull
                && _characters[1].Group.IsGroupFull
                && _characters[2].Group.IsGroupFull);

            await _pLeavePacketHandler!.Execute(new PleavePacket(), _characters[1].Session);

            Assert.IsTrue(_characters[1].Group.Count == 1);
        }

        [TestMethod]
        public async Task Test_Leader_Change()
        {
            for (var i = 1; i < 3; i++)
            {
                _characters[i].GroupRequestCharacterIds
                    .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = _characters[i].CharacterId
                };

                await _pJoinPacketHandler!.Execute(pjoinPacket, _characters[0].Session);
            }

            Assert.IsTrue(_characters[0].Group.IsGroupFull
                && _characters[1].Group.IsGroupFull
                && _characters[2].Group.IsGroupFull && _characters[0].Group
                    .IsGroupLeader(_characters[0].CharacterId));

            await _pLeavePacketHandler!.Execute(new PleavePacket(), _characters[0].Session);

            Assert.IsTrue(_characters[1].Group
                .IsGroupLeader(_characters[1].CharacterId));
        }

        [TestMethod]
        public async Task Test_Leaving_Three_Person_Group()
        {
            for (var i = 1; i < 3; i++)
            {
                _characters[i].GroupRequestCharacterIds
                    .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = _characters[i].CharacterId
                };

                await _pJoinPacketHandler!.Execute(pjoinPacket, _characters[0].Session);
            }

            Assert.IsTrue(_characters[0].Group.IsGroupFull
                && _characters[1].Group.IsGroupFull
                && _characters[2].Group.IsGroupFull && _characters[0].Group
                    .IsGroupLeader(_characters[0].CharacterId));

            await _pLeavePacketHandler!.Execute(new PleavePacket(), _characters[1].Session);

            Assert.IsTrue(_characters[0].Group
                .IsGroupLeader(_characters[0].CharacterId));
        }

        [TestMethod]
        public async Task Test_Decline_Group_Join_Requested()
        {
            _characters[1].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Declined,
                CharacterId = _characters[1].CharacterId
            };

            await _pJoinPacketHandler!.Execute(pjoinPacket, _characters[0].Session);
            await _pLeavePacketHandler!.Execute(new PleavePacket(), _characters[0].Session);
            Assert.IsTrue((_characters[0].Group.Count == 1)
                && (_characters[1].Group.Count == 1));
        }

        [TestMethod]
        public async Task Test_Leaving_Two_Person_Group()
        {
            _characters[1].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[1].CharacterId
            };

            await _pJoinPacketHandler!.Execute(pjoinPacket, _characters[0].Session);
            Assert.IsTrue((_characters[0].Group.Count > 1)
                && (_characters[1].Group.Count > 1)
                && (_characters[0].Group.GroupId
                    == _characters[1].Group.GroupId));

            await _pLeavePacketHandler!.Execute(new PleavePacket(), _characters[0].Session);

            Assert.IsTrue((_characters[0].Group.Count == 1)
                && (_characters[1].Group.Count == 1));
        }
    }
}