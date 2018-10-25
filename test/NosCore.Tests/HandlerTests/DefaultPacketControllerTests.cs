using System;
using System.Collections.Generic;
using System.Linq;
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
using NosCore.GameObject.Services.PortalGeneration;
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
        private ClientSession _targetSession = new ClientSession(null, new List<PacketController> { new DefaultPacketController() }, null);
        private CharacterDto _chara;
        private CharacterDto _targetChar;
        private DefaultPacketController _handler;
        private readonly Map _map = new Map
        {
            MapId = 0,
            Name = "testMap",
            Data = new byte[]
                {
                    8, 0, 8, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0
                }
        };
        private readonly Map _map2 = new Map
        {
            MapId = 1,
            Name = "testMap2",
            Data = new byte[]
            {
                8, 0, 8, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            }
        };

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

            var _chara = new CharacterDto
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

            var instanceAccessService = new MapInstanceAccessService(new List<NpcMonsterDto>(), new List<Map>() { _map, _map2 });
            _session = new ClientSession(null, new List<PacketController> { new DefaultPacketController(null, instanceAccessService) }, instanceAccessService);
            _targetSession = new ClientSession(null, new List<PacketController> { new DefaultPacketController(null, instanceAccessService) }, instanceAccessService);
            var channelMock = new Mock<IChannel>();
            _session.RegisterChannel(channelMock.Object);
            _targetSession.RegisterChannel(channelMock.Object);
            _session.InitializeAccount(account);
            _session.SessionId = 1;
            _handler = new DefaultPacketController(null, instanceAccessService);
            _handler.RegisterSession(_session);

            _targetSession.InitializeAccount(targetAccount);
            _targetSession.SessionId = 2;
            var _handler2 = new DefaultPacketController(null, instanceAccessService);
            _handler2.RegisterSession(_targetSession);

            DaoFactory.CharacterDao.InsertOrUpdate(ref _targetChar);
            _targetSession.InitializeAccount(targetAccount);

            _session.SetCharacter(_chara.Adapt<Character>());
            var mapinstance = instanceAccessService.GetBaseMapById(0);
            _session.Character.MapInstance = instanceAccessService.GetBaseMapById(0);

            _targetSession.SetCharacter(_targetChar.Adapt<Character>());
            _targetSession.Character.MapInstance = new MapInstance(new Map(), Guid.NewGuid(), true, MapInstanceType.BaseMapInstance, null);
            _session.Character.CharacterId = 1;
            _targetSession.Character.CharacterId = 2;

            Broadcaster.Instance.RegisterSession(_session);
            Broadcaster.Instance.RegisterSession(_targetSession);
        }

        [TestMethod]
        public void UserCanUsePortal()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _handler.Preq(new PreqPacket());
            Assert.IsTrue(_session.Character.PositionY == 5 && _session.Character.PositionX == 5 && _session.Character.MapInstance.Map.MapId == 1);
        }

        [TestMethod]
        public void UserCanTUsePortalIfTooFar()
        {
            _session.Character.PositionX = 8;
            _session.Character.PositionY = 8;
            _handler.Preq(new PreqPacket());
            Assert.IsTrue(_session.Character.PositionY == 8 && _session.Character.PositionX == 8 && _session.Character.MapInstance.Map.MapId == 0);
        }

        [TestMethod]
        public void Test_Add_Friend()
        {
            _targetSession.Character.FriendRequestCharacters.TryAdd(0, _session.Character.CharacterId);
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            _handler.AddFriend(finsPacket);
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _targetSession.Character.CharacterId)
                && _targetSession.Character.CharacterRelations.Any(s => s.Value.RelatedCharacterId == _session.Character.CharacterId));
        }

        [TestMethod]
        public void Test_Add_Friend_When_Disconnected()
        {
            _targetSession.Disconnect();
            Broadcaster.Instance.UnregisterSession(_targetSession);
            _targetSession = null;

            var finsPacket = new FinsPacket
            {
                CharacterId = _targetChar.CharacterId,
                Type = FinsPacketType.Accepted
            };
            _handler.AddFriend(finsPacket);

            Assert.IsTrue(_session.Character.CharacterRelations.IsEmpty);
        }

        [TestMethod]
        public void Test_Blacklist_When_Disconnected()
        {
            var charId = _targetSession.Character.CharacterId;
            _targetSession.Disconnect();
            Broadcaster.Instance.UnregisterSession(_targetSession);
            _targetSession = null;

            var blinsPacket = new BlInsPacket
            {
                CharacterId = charId
            };

            _handler.BlackListAdd(blinsPacket);
            Assert.IsFalse(_session.Character.CharacterRelations.Any(s => s.Value.RelationType == CharacterRelationType.Blocked));
        }

        [TestMethod]
        public void Test_Delete_Friend_When_Disconnected()
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

            _targetSession.Disconnect();
            Broadcaster.Instance.UnregisterSession(_targetSession);
            _targetSession = null;

            _handler.DeleteFriend(fdelPacket);

            Assert.IsTrue(_session.Character.CharacterRelations.IsEmpty);
        }

        [TestMethod]
        public void Test_Delete_Blacklist_When_Disconnected()
        {
            var charId = _targetSession.Character.CharacterId;
            var blinsPacket = new BlInsPacket
            {
                CharacterId = _targetSession.Character.CharacterId
            };

            _handler.BlackListAdd(blinsPacket);

            _targetSession.Disconnect();
            Broadcaster.Instance.UnregisterSession(_targetSession);
            _targetSession = null;

            var bldelPacket = new BlDelPacket
            {
                CharacterId = charId
            };

            _handler.BlackListDelete(bldelPacket);
            Assert.IsTrue(_session.Character.CharacterRelations.All(s => s.Value.RelatedCharacterId != charId));
        }

        [TestMethod]
        public void Test_Add_Not_Requested_Friend()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetChar.CharacterId,
                Type = FinsPacketType.Accepted
            };
            _handler.AddFriend(finsPacket);
            Assert.IsTrue(_session.Character.CharacterRelations.IsEmpty && _targetSession.Character.CharacterRelations.IsEmpty);
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

        [TestMethod]
        public void Test_Pulse_Packet()
        {
            var pulsePacket = new PulsePacket
            {
                Tick = 0
            };

            for (var i = 60; i < 600; i += 60)
            {
                pulsePacket = new PulsePacket
                {
                    Tick = i
                };

                _handler.Pulse(pulsePacket);
            }

            Assert.IsTrue(_session.LastPulse == pulsePacket.Tick);
        }
    }
}
