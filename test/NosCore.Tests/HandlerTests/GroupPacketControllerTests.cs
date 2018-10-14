using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using log4net.Config;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Group;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class GroupPacketControllerTests
    {
        private const string ConfigurationPath = "../../../configuration";
        private readonly List<GroupPacketController> _handlers = new List<GroupPacketController>();

        [TestInitialize]
        public void Setup()
        {
            var accountList = new List<AccountDTO>();
            var characterList = new List<CharacterDTO>();
            PacketFactory.Initialize<NoS0575Packet>();
            var contextBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(ConfigurationPath + "/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(GroupPacketControllerTests)));
            var map = new MapDTO { MapId = 1 };
            DAOFactory.MapDAO.InsertOrUpdate(ref map);

            ServerManager.Instance.Sessions = new ConcurrentDictionary<long, ClientSession>();
            ServerManager.Instance.Groups = new ConcurrentDictionary<long, Group>();
            for (byte i = 0; i < (byte)(GroupType.Group + 1); i++)
            {
                ServerManager.Instance.Sessions.TryAdd(i, new ClientSession(null, new List<PacketController>() { new GroupPacketController() }, null));
                var session = ServerManager.Instance.Sessions.Values.ElementAt(i);

                accountList.Add(new AccountDTO { Name = $"AccountTest{i}", Password = EncryptionHelper.Sha512("test") });
                var acc = accountList.ElementAt(i);
                DAOFactory.AccountDAO.InsertOrUpdate(ref acc);

                characterList.Add(new CharacterDTO
                {
                    Name = $"TestExistingCharacter{i}",
                    Slot = 1,
                    AccountId = acc.AccountId,
                    MapId = 1,
                    State = CharacterState.Active
                });

                var charaDto = characterList.ElementAt(i);

                DAOFactory.CharacterDAO.InsertOrUpdate(ref charaDto);
                session.InitializeAccount(acc);
                _handlers.Add(new GroupPacketController());
                var handler = _handlers.ElementAt(i);
                handler.RegisterSession(session);

                var chara = charaDto.Adapt<Character>();

                chara.Group.JoinGroup(chara);
                session.SetCharacter(chara);
                session.Character.MapInstance = new MapInstance(new Map(), Guid.NewGuid(), true, MapInstanceType.BaseMapInstance, null);
            }
        }

        [TestMethod]
        public void Test_Accept_Group_Join()
        {
            ServerManager.Instance.Sessions.Values.ElementAt(1).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(1).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.Count > 1 && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.Count > 1 && ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.GroupId == ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.GroupId);
        }

        [TestMethod]
        public void Test_Join_Full_Group()
        {
            PjoinPacket pjoinPacket;

            for (var i = 1; i < 3; i++)
            {
                ServerManager.Instance.Sessions.Values.ElementAt(i).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

                pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(i).Character.CharacterId
                };

                _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            }

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(2).Character.Group.IsGroupFull);

            ServerManager.Instance.Sessions.Values.ElementAt(3).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(3).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(3).Character.Group.Count == 1);
        }

        [TestMethod]
        public void Test_Leave_Group()
        {
            for (var i = 1; i < 3; i++)
            {
                ServerManager.Instance.Sessions.Values.ElementAt(i).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(i).Character.CharacterId
                };

                _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            }

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(2).Character.Group.IsGroupFull);

            _handlers.ElementAt(1).LeaveGroup(new PleavePacket());

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.Count == 1);
        }

        [TestMethod]
        public void Test_Leader_Change()
        {
            for (var i = 1; i < 3; i++)
            {
                ServerManager.Instance.Sessions.Values.ElementAt(i).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(i).Character.CharacterId
                };

                _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            }

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(2).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupLeader(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId));

            _handlers.ElementAt(0).LeaveGroup(new PleavePacket());

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsGroupLeader(ServerManager.Instance.Sessions.Values.ElementAt(1).Character.CharacterId));
        }

        [TestMethod]
        public void Test_Leaveing_Two_Person_Group()
        {
            for (var i = 1; i < 3; i++)
            {
                ServerManager.Instance.Sessions.Values.ElementAt(i).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

                var pjoinPacket = new PjoinPacket
                {
                    RequestType = GroupRequestType.Accepted,
                    CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(i).Character.CharacterId
                };

                _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            }

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(2).Character.Group.IsGroupFull && ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupLeader(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId));

            _handlers.ElementAt(1).LeaveGroup(new PleavePacket());

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.IsGroupLeader(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId));
        }

        [TestMethod]
        public void Test_Rejected_Group_Join()
        {
            ServerManager.Instance.Sessions.Values.ElementAt(1).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Declined,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(1).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.Count == 1 && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.Count == 1);
        }

        [TestMethod]
        public void Test_Leaving_Two_Person_Group()
        {
            ServerManager.Instance.Sessions.Values.ElementAt(1).Character.GroupRequestCharacterIds.Add(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.CharacterId);

            var pjoinPacket = new PjoinPacket
            {
                RequestType = GroupRequestType.Accepted,
                CharacterId = ServerManager.Instance.Sessions.Values.ElementAt(1).Character.CharacterId
            };

            _handlers.ElementAt(0).ManageGroup(pjoinPacket);
            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.Count > 1 && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.Count > 1 && ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.GroupId == ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.GroupId);

            _handlers.ElementAt(0).LeaveGroup(new PleavePacket());

            Assert.IsTrue(ServerManager.Instance.Sessions.Values.ElementAt(0).Character.Group.Count == 1 && ServerManager.Instance.Sessions.Values.ElementAt(1).Character.Group.Count == 1);
        }
    }
}
