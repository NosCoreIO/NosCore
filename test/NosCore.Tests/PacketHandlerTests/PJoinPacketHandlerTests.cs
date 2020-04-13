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
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Groups;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
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
    public class PJoinPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private readonly Dictionary<int, Character> _characters = new Dictionary<int, Character>();
        private PjoinPacketHandler? _pJoinPacketHandler;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(SystemTime.Now());
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            SystemTime.Freeze();
            Broadcaster.Reset();
            GroupAccess.Instance.Groups = new ConcurrentDictionary<long, Group>();
            for (byte i = 0; i < (byte) (GroupType.Group + 1); i++)
            {
                var session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
                session.RegisterChannel(null);
                _characters.Add(i, session.Character);
                session.Character.Group!.JoinGroup(session.Character);
            }

            var mock = new Mock<IBlacklistHttpClient>();
            _pJoinPacketHandler = new PjoinPacketHandler(Logger, mock.Object);
        }

        [TestMethod]
        public async Task Test_Accept_Group_Join_RequestedAsync()
        {
            _characters[1].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[1].CharacterId
            };

            await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _characters[0].Session).ConfigureAwait(false);
            Assert.IsTrue((_characters[0].Group!.Count > 1)
                && (_characters[1].Group!.Count > 1)
                && (_characters[0].Group!.GroupId
                    == _characters[1].Group!.GroupId));
        }

        [TestMethod]
        public async Task Test_Join_Full_GroupAsync()
        {
            PjoinPacket pjoinPacket;

            for (var i = 1; i < 3; i++)
            {
                _characters[i].GroupRequestCharacterIds
                    .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

                pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = _characters[i].CharacterId
                };

                await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _characters[0].Session).ConfigureAwait(false);
            }

            Assert.IsTrue(_characters[0].Group!.IsGroupFull
                && _characters[1].Group!.IsGroupFull
                && _characters[2].Group!.IsGroupFull);

            _characters[3].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[3].CharacterId
            };

            await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _characters[0].Session).ConfigureAwait(false);
            Assert.IsTrue(_characters[3].Group!.Count == 1);
        }

        [TestMethod]
        public async Task Test_Accept_Not_Requested_GroupAsync()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[1].CharacterId
            };

            await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _characters[0].Session).ConfigureAwait(false);
            Assert.IsTrue((_characters[0].Group!.Count == 1)
                && (_characters[1].Group!.Count == 1));
        }

        [TestMethod]
        public async Task Test_Decline_Not_Requested_GroupAsync()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Declined,
                CharacterId = _characters[1].CharacterId
            };

            await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _characters[0].Session).ConfigureAwait(false);
            Assert.IsTrue((_characters[0].Group!.Count == 1)
                && (_characters[1].Group!.Count == 1));
        }

        [TestMethod]
        public async Task Test_Last_Request_Not_Null_After_OneAsync()
        {
            for (var i = 1; i < 3; i++)
            {
                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Invited,
                    CharacterId = _characters[i].CharacterId
                };

                await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _characters[0].Session).ConfigureAwait(false);
            }
            Assert.IsNotNull(_characters[0].LastGroupRequest);
        }

        [TestMethod]
        public async Task Test_Two_Request_Less_5_Sec_DelayAsync()
        {
            SystemTime.Freeze(SystemTime.Now());
            for (var i = 1; i < 3; i++)
            {
                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Invited,
                    CharacterId = _characters[i].CharacterId
                };

                SystemTime.Freeze(SystemTime.Now().AddSeconds(1));
                await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _characters[0].Session).ConfigureAwait(false);
            }

            Assert.IsTrue(_characters[0].GroupRequestCharacterIds.Count == 1);
        }

        [TestMethod]
        public async Task Test_Two_Request_More_5_Sec_DelayAsync()
        {
            for (var i = 1; i < 3; i++)
            {
                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Invited,
                    CharacterId = _characters[i].CharacterId
                };

                if (i == 2)
                {
                    SystemTime.Freeze(SystemTime.Now().AddSeconds(6));
                }

                await _pJoinPacketHandler!.ExecuteAsync(pjoinPacket, _characters[0].Session).ConfigureAwait(false);
            }

            Assert.IsTrue(_characters[0].GroupRequestCharacterIds.Count == 2);
        }
    }
}