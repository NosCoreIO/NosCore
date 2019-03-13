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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class GroupPacketControllerTests
    {
        private readonly Dictionary<int, Character> _characters = new Dictionary<int, Character>();
        private readonly List<GroupPacketController> _handlers = new List<GroupPacketController>();

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            Broadcaster.Reset();
            GroupAccess.Instance.Groups = new ConcurrentDictionary<long, Group>();
            for (byte i = 0; i < (byte)(GroupType.Group + 1); i++)
            {
                var handler = new GroupPacketController();
                var session = new ClientSession(null, new List<PacketController> { handler }, null, null) { SessionId = i };

                Broadcaster.Instance.RegisterSession(session);
                var acc = new AccountDto { Name = $"AccountTest{i}", Password = "test".ToSha512() };
                var charaDto = new Character(null, null, null, null, null, null, null)
                {
                    CharacterId = i,
                    Name = $"TestExistingCharacter{i}",
                    Slot = 1,
                    AccountId = acc.AccountId,
                    MapId = 1,
                    State = CharacterState.Active
                };

                session.InitializeAccount(acc);
                _handlers.Add(handler);
                handler.RegisterSession(session);

                var chara = charaDto;
                chara.Session = session;
                _characters.Add(i, chara);
                chara.Group.JoinGroup(chara);
                session.SetCharacter(chara);
                session.Character.MapInstance = new MapInstance(new Map(), Guid.NewGuid(), true,
                    MapInstanceType.BaseMapInstance,
                     new MapItemProvider(new List<IHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                    null);
            }
        }

        [TestMethod]
        public void Test_Accept_Group_Join_Requested()
        {
            _characters[1].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[1].CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(_characters[0].Group.Count > 1
                && _characters[1].Group.Count > 1
                && _characters[0].Group.GroupId
                == _characters[1].Group.GroupId);
        }

        [TestMethod]
        public void Test_Join_Full_Group()
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

                _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            }

            Assert.IsTrue(_characters[0].Group.IsGroupFull
                && _characters[1].Group.IsGroupFull
                && _characters[2].Group.IsGroupFull);

            _characters[3].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[3].CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(_characters[3].Group.Count == 1);
        }

        [TestMethod]
        public void Test_Accept_Not_Requested_Group()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[1].CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(_characters[0].Group.Count == 1
                && _characters[1].Group.Count == 1);
        }

        [TestMethod]
        public void Test_Decline_Not_Requested_Group()
        {
            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Declined,
                CharacterId = _characters[1].CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(_characters[0].Group.Count == 1
                && _characters[1].Group.Count == 1);
        }

        [TestMethod]
        public void Test_Leave_Group_When_Not_Grouped()
        {
            _handlers.ElementAt(0).LeaveGroup(new PleavePacket());

            Assert.IsTrue(_characters[0].Group != null && _characters[0].Group.Count == 1);
        }

        [TestMethod]
        public void Test_Leave_Group_When_Grouped()
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

                _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            }

            Assert.IsTrue(_characters[0].Group.IsGroupFull
                && _characters[1].Group.IsGroupFull
                && _characters[2].Group.IsGroupFull);

            _handlers.ElementAt(1).LeaveGroup(new PleavePacket());

            Assert.IsTrue(_characters[1].Group.Count == 1);
        }

        [TestMethod]
        public void Test_Leader_Change()
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

                _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            }

            Assert.IsTrue(_characters[0].Group.IsGroupFull
                && _characters[1].Group.IsGroupFull
                && _characters[2].Group.IsGroupFull && _characters[0].Group
                    .IsGroupLeader(_characters[0].CharacterId));

            _handlers.ElementAt(0).LeaveGroup(new PleavePacket());

            Assert.IsTrue(_characters[1].Group
                .IsGroupLeader(_characters[1].CharacterId));
        }

        [TestMethod]
        public void Test_Leaving_Three_Person_Group()
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

                _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            }

            Assert.IsTrue(_characters[0].Group.IsGroupFull
                && _characters[1].Group.IsGroupFull
                && _characters[2].Group.IsGroupFull && _characters[0].Group
                    .IsGroupLeader(_characters[0].CharacterId));

            _handlers.ElementAt(1).LeaveGroup(new PleavePacket());

            Assert.IsTrue(_characters[0].Group
                .IsGroupLeader(_characters[0].CharacterId));
        }

        [TestMethod]
        public void Test_Decline_Group_Join_Requested()
        {
            _characters[1].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Declined,
                CharacterId = _characters[1].CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(_characters[0].Group.Count == 1
                && _characters[1].Group.Count == 1);
        }

        [TestMethod]
        public void Test_Leaving_Two_Person_Group()
        {
            _characters[1].GroupRequestCharacterIds
                .TryAdd(_characters[0].CharacterId, _characters[0].CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[1].CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(_characters[0].Group.Count > 1
                && _characters[1].Group.Count > 1
                && _characters[0].Group.GroupId
                == _characters[1].Group.GroupId);

            _handlers.ElementAt(0).LeaveGroup(new PleavePacket());

            Assert.IsTrue(_characters[0].Group.Count == 1
                && _characters[1].Group.Count == 1);
        }
    }
}