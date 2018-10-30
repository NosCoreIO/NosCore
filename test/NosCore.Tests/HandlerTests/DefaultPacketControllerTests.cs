using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Map;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class DefaultPacketControllerTests
    {
        private readonly ClientSession _session = new ClientSession(null, new List<PacketController> { new DefaultPacketController() }, null);
        private readonly ClientSession _targetSession = new ClientSession(null, new List<PacketController> { new DefaultPacketController() }, null);
        private CharacterDto _chara;
        private CharacterDto _targetChar;
        private DefaultPacketController _handler;
        private DefaultPacketController _handler2;

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            Broadcaster.Reset();
            var contextBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto { MapId = 1 };
            DaoFactory.MapDao.InsertOrUpdate(ref map);
            var account = new AccountDto { Name = "AccountTest", Password = EncryptionHelper.Sha512("test") };
            var targetAccount = new AccountDto { Name = "test2", Password = EncryptionHelper.Sha512("test") };
            DaoFactory.AccountDao.InsertOrUpdate(ref account);
            DaoFactory.AccountDao.InsertOrUpdate(ref targetAccount);
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues =
                new Dictionary<string, object>
                {
                    { "api/channels", new List<WorldServerInfo> { new WorldServerInfo() } },
                    { "api/connectedAccount", new List<ConnectedAccount>() }
                };

            _chara = new CharacterDto
            {
                CharacterId = 0,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = account.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            _targetChar = new CharacterDto
            {
                CharacterId = 1,
                Name = "TestChar2",
                Slot = 1,
                AccountId = targetAccount.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            DaoFactory.CharacterDao.InsertOrUpdate(ref _chara);
            _session.InitializeAccount(account);
            _session.SessionId = 1;
            _handler = new DefaultPacketController(null, null);
            _handler.RegisterSession(_session);

            _targetSession.InitializeAccount(targetAccount);
            _targetSession.SessionId = 2;
            _handler2 = new DefaultPacketController(null, null);
            _handler2.RegisterSession(_targetSession);

            DaoFactory.CharacterDao.InsertOrUpdate(ref _targetChar);
            _targetSession.InitializeAccount(targetAccount);

            _session.SetCharacter(_chara.Adapt<Character>());
            _session.Character.MapInstance = new MapInstance(new Map(), Guid.NewGuid(), true, MapInstanceType.BaseMapInstance, null);

            _targetSession.SetCharacter(_targetChar.Adapt<Character>());
            _targetSession.Character.MapInstance = new MapInstance(new Map(), Guid.NewGuid(), true, MapInstanceType.BaseMapInstance, null);

            Broadcaster.Instance.RegisterSession(_session);
            Broadcaster.Instance.RegisterSession(_targetSession);
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
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _targetSession.Character.CharacterId)
                && _targetSession.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _session.Character.CharacterId));
        }

        //TODO add add friend when disconnected
        //TODO add add blacklist when disconnected
        //TODO add delete friend when disconnected
        //TODO add delete blacklist when disconnected

        [TestMethod]
        public void Test_Add_Distant_Friend()
        {
            _targetSession.Character.FriendRequestCharacters.TryAdd(0, _session.Character.CharacterId);
            var flPacket = new FlPacket
            {
                CharacterName = _targetSession.Character.Name
            };

            _handler.AddDistantFriend(flPacket);
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _targetSession.Character.CharacterId)
                && _targetSession.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _session.Character.CharacterId));
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
