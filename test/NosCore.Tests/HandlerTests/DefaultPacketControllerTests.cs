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
using NosCore.GameObject.Services;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class DefaultPacketControllerTests
    {
        private const string ConfigurationPath = "../../../configuration";
        private readonly ClientSession _session = new ClientSession(null, new List<PacketController>() { new DefaultPacketController() }, null);
        private ClientSession _targetSession = new ClientSession(null, new List<PacketController>() { new DefaultPacketController() }, null);
        private AccountDTO _acc;
        private AccountDTO _targetAcc;
        private CharacterDTO _chara;
        private CharacterDTO _targetChar;
        private DefaultPacketController _handler;
        private DefaultPacketController _targetHandler;

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var builder = new ConfigurationBuilder();
            var contextBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(ConfigurationPath + "/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(DefaultPacketControllerTests)));
            var map = new MapDTO { MapId = 1 };
            DAOFactory.MapDAO.InsertOrUpdate(ref map);
            _acc = new AccountDTO { Name = "AccountTest", Password = EncryptionHelper.Sha512("test") };
            _targetAcc = new AccountDTO { Name = "test2", Password = EncryptionHelper.Sha512("test") };
            DAOFactory.AccountDAO.InsertOrUpdate(ref _acc);
            DAOFactory.AccountDAO.InsertOrUpdate(ref _targetAcc);

            _chara = new CharacterDTO
            {
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = _acc.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            _targetChar = new CharacterDTO
            {
                Name = "TestChar2",
                Slot = 1,
                AccountId = _targetAcc.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            DAOFactory.CharacterDAO.InsertOrUpdate(ref _chara);
            _session.InitializeAccount(_acc);
            _handler = new DefaultPacketController(null, null);
            _handler.RegisterSession(_session);

            DAOFactory.CharacterDAO.InsertOrUpdate(ref _targetChar);
            _targetSession.InitializeAccount(_targetAcc);
            _targetHandler = new DefaultPacketController(null, null);
            _targetHandler.RegisterSession(_targetSession);

            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues =
                new Dictionary<string, object>
                {
                    { "api/channels", new List<WorldServerInfo> { new WorldServerInfo() } },
                    { "api/connectedAccount", new List<ConnectedAccount>() }
                };

            _session.SetCharacter(_chara.Adapt<Character>());
            _session.Character.MapInstance = new MapInstance(new Map(), Guid.NewGuid(), true, MapInstanceType.BaseMapInstance, null);
            _targetSession.SetCharacter(_targetChar.Adapt<Character>());
            _targetSession.Character.MapInstance = new MapInstance(new Map(), Guid.NewGuid(), true, MapInstanceType.BaseMapInstance, null);
            ServerManager.Instance.Sessions = new ConcurrentDictionary<long, ClientSession>();
            ServerManager.Instance.Sessions.TryAdd(_chara.CharacterId, _session);
            ServerManager.Instance.Sessions.TryAdd(_targetChar.CharacterId, _targetSession);
        }

        [TestMethod]
        public void Test_Add_Friend()
        {
            _targetSession.Character.FriendRequestCharacters.TryAdd(0, _session.Character.CharacterId);
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetChar.CharacterId,
                Type = FinsPacketType.Accepted
            };
            _handler.AddFriend(finsPacket);
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _targetSession.Character.CharacterId) &&
                _targetSession.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _session.Character.CharacterId));
        }

        [TestMethod]
        public void Test_Add_Distant_Friend()
        {
            _targetSession.Character.FriendRequestCharacters.TryAdd(0, _session.Character.CharacterId);
            var flPacket = new FlPacket
            {
                CharacterName = _targetSession.Character.Name
            };

            _handler.AddDistantFriend(flPacket);
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _targetSession.Character.CharacterId) &&
                _targetSession.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _session.Character.CharacterId));
        }

        [TestMethod]
        public void Test_Delete_Friend()
        {
            var fdelPacket = new FdelPacket
            {
                CharacterId = _targetChar.CharacterId
            };

            _targetSession.Character.FriendRequestCharacters.TryAdd(0, _session.Character.CharacterId);
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetChar.CharacterId,
                Type = FinsPacketType.Accepted
            };
            _handler.AddFriend(finsPacket);

            _handler.DeleteFriend(fdelPacket);
            Assert.IsTrue(_session.Character.CharacterRelations.All(s => s.Value.RelatedCharacterId != _targetSession.Character.CharacterId) 
                && _targetSession.Character.CharacterRelations.All(s => s.Value.RelatedCharacterId != _session.Character.CharacterId));
        }

        [TestMethod]
        public void Test_Blacklist_Character()
        {
            var blinsPacket = new BlInsPacket
            {
                CharacterId = _targetSession.Character.CharacterId
            };

            _handler.BlackListAdd(blinsPacket);
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _targetSession.Character.CharacterId
                && s.Value.RelationType == CharacterRelationType.Blocked));
        }

        [TestMethod]
        public void Test_Distant_Blacklist()
        {
            var blPacket = new BlPacket
            {
                CharacterName = _targetSession.Character.Name
            };

            _handler.DistantBlackList(blPacket);
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _targetSession.Character.CharacterId
                && s.Value.RelationType == CharacterRelationType.Blocked));
        }

        [TestMethod]
        public void Test_Delete_Blacklist()
        {
            var blinsPacket = new BlInsPacket
            {
                CharacterId = _targetSession.Character.CharacterId
            };

            _handler.BlackListAdd(blinsPacket);

            var bldelPacket = new BlDelPacket
            {
                CharacterId = _targetSession.Character.CharacterId
            };

            _handler.BlackListDelete(bldelPacket);
            Assert.IsTrue(_session.Character.CharacterRelations.All(s => s.Value.RelatedCharacterId != _targetSession.Character.CharacterId));
        }
    }
}
