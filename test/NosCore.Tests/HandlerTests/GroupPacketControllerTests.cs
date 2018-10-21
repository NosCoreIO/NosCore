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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Config;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Group;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class GroupPacketControllerTests
    {
        private const string ConfigurationPath = "../../../configuration";
        private readonly List<GroupPacketController> _handlers = new List<GroupPacketController>();
        private readonly Dictionary<int, Character> _characters = new Dictionary<int, Character>();

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(ConfigurationPath + "/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(GroupPacketControllerTests)));
            var map = new MapDto { MapId = 1 };
            DaoFactory.MapDao.InsertOrUpdate(ref map);

            Broadcaster.Instance.ClientSessions = new ConcurrentDictionary<long, ClientSession>();
            GroupAccess.Instance.Groups = new ConcurrentDictionary<long, Group>();
            for (byte i = 0; i < (byte)(GroupType.Group + 1); i++)
            {
                var session = new ClientSession(null, new List<PacketController> { new GroupPacketController() }, null);
                Broadcaster.Instance.ClientSessions.TryAdd(i, session);
                var acc = new AccountDto { Name = $"AccountTest{i}", Password = EncryptionHelper.Sha512("test") };
                var charaDto = new CharacterDto
                {
                    CharacterId = i,
                    Name = $"TestExistingCharacter{i}",
                    Slot = 1,
                    AccountId = acc.AccountId,
                    MapId = 1,
                    State = CharacterState.Active
                };

                session.InitializeAccount(acc);
                var handler = new GroupPacketController();
                _handlers.Add(handler);
                handler.RegisterSession(session);

                var chara = charaDto.Adapt<Character>();
                _characters.Add(i, chara);
                chara.Group.JoinGroup(chara);
                session.SetCharacter(chara);
                session.Character.MapInstance = new MapInstance(new Map(), Guid.NewGuid(), true, MapInstanceType.BaseMapInstance, null);
            }
        }

        [TestMethod]
        public void Test_Accept_Group_Join_Requested()
        {
            _characters[1].GroupRequestCharacterIds
                .Add(_characters[0].CharacterId);

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
                    .Add(_characters[0].CharacterId);

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
                .Add(_characters[0].CharacterId);

            pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = _characters[3].CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(_characters[3].Group.Count == 1);
        }
        //public void Test_Accept_Not_Requested_Group()
        //public void Test_Decline_Not_Requested_Group()
        //public void Test_Leave_Group_When_NotGrouped()
        [TestMethod]
        public void Test_Leave_Group_When_Grouped()
        {
            for (var i = 1; i < 3; i++)
            {
                _characters[i].GroupRequestCharacterIds
                    .Add(_characters[0].CharacterId);

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

        //TODO create a similar test for the object group
        [TestMethod]
        public void Test_Leader_Change()
        {
            for (var i = 1; i < 3; i++)
            {
                _characters[i].GroupRequestCharacterIds
                    .Add(_characters[0].CharacterId);

                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = _characters[i].CharacterId
                };

                _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            }

            Assert.IsTrue(_characters[0].Group.IsGroupFull
                && _characters[1].Group.IsGroupFull
                && _characters[2].Group.IsGroupFull && Broadcaster
                    .Instance.ClientSessions.Values.ElementAt(0).Character.Group
                    .IsGroupLeader(_characters[0].CharacterId));

            _handlers.ElementAt(0).LeaveGroup(new PleavePacket());

            Assert.IsTrue(_characters[1].Group
                .IsGroupLeader(_characters[1].CharacterId));
        }

        [TestMethod]
        public void Test_Leaveing_Two_Person_Group()
        {
            for (var i = 1; i < 3; i++)
            {
                _characters[i].GroupRequestCharacterIds
                    .Add(_characters[0].CharacterId);

                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = _characters[i].CharacterId
                };

                _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            }

            Assert.IsTrue(_characters[0].Group.IsGroupFull
                && _characters[1].Group.IsGroupFull
                && _characters[2].Group.IsGroupFull && Broadcaster
                    .Instance.ClientSessions.Values.ElementAt(0).Character.Group
                    .IsGroupLeader(_characters[0].CharacterId));

            _handlers.ElementAt(1).LeaveGroup(new PleavePacket());

            Assert.IsTrue(_characters[0].Group
                .IsGroupLeader(_characters[0].CharacterId));
        }

        [TestMethod]
        public void Test_Decline_Group_Join_Requested()
        {
            _characters[1].GroupRequestCharacterIds
                .Add(_characters[0].CharacterId);

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
                .Add(_characters[0].CharacterId);

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