using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.ClientPackets.Movement;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Login;
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.PacketHandlers.Friend;
using NosCore.PacketHandlers.Login;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class FinsPacketHandlerTests
    {
        private FinsPacketHandler _finsPacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IGenericDao<CharacterDto> _characterDao = new GenericDao<Database.Entities.Character, CharacterDto>(_logger);
        private readonly IGenericDao<AccountDto> _accountDao = new GenericDao<Database.Entities.Account, AccountDto>(_logger);
        private readonly IGenericDao<MapDto> _mapDao = new GenericDao<Database.Entities.Map, MapDto>(_logger);
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao = new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);
        private readonly IGenericDao<PortalDto> _portalDao = new GenericDao<Database.Entities.Portal, PortalDto>(_logger);
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao = new ItemInstanceDao(_logger);
        private readonly IGenericDao<MapMonsterDto> _mapMonsterDao = new GenericDao<Database.Entities.MapMonster, MapMonsterDto>(_logger);
        private readonly IGenericDao<MapNpcDto> _mapNpcDao = new GenericDao<Database.Entities.MapNpc, MapNpcDto>(_logger);

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

        private ClientSession _session;
        private Character _targetChar;
        private ClientSession _targetSession;

        [TestInitialize]
        public void Setup()
        {
            var conf = new WorldConfiguration();
            TypeAdapterConfig<MapMonsterDto, MapMonster>.NewConfig().ConstructUsing(src => new MapMonster(new List<NpcMonsterDto>(), _logger));
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig().ConstructUsing(src => new MapNpc(null, null, null, null, _logger));
            Broadcaster.Reset();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto { MapId = 1 };
            _mapDao.InsertOrUpdate(ref map);
            var account = new AccountDto { Name = "AccountTest", Password = "test".ToSha512() };
            _accountDao.InsertOrUpdate(ref account);
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues =
                new Dictionary<WebApiRoute, object>
                {
                    {WebApiRoute.Channel, new List<ChannelInfo> {new ChannelInfo()}},
                    {WebApiRoute.ConnectedAccount, new List<ConnectedAccount>()}
                };

            var _chara = new Character(null, null, null, _characterRelationDao, _characterDao, _itemInstanceDao, _accountDao, _logger, null)
            {
                CharacterId = 1,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = account.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            var instanceAccessService = new MapInstanceProvider(new List<MapDto> { _map, _map2 },
                new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                _mapNpcDao,
                _mapMonsterDao, _portalDao, new Adapter(), _logger);
            instanceAccessService.Initialize();
            var channelMock = new Mock<IChannel>();
            _session = new ClientSession(conf, instanceAccessService, null, _logger, new List<IPacketHandler>());
            _session.RegisterChannel(channelMock.Object);
            _session.InitializeAccount(account);
            _session.SessionId = 1;
            _session.SetCharacter(_chara);
            var mapinstance = instanceAccessService.GetBaseMapById(0);

            _session.Character.MapInstance = instanceAccessService.GetBaseMapById(0);
            _session.Character.MapInstance = mapinstance;
            _session.Character.MapInstance.Portals = new List<Portal>
            {
                new Portal
                {
                    DestinationMapId = _map2.MapId,
                    Type = PortalType.Open,
                    SourceMapInstanceId = mapinstance.MapInstanceId,
                    DestinationMapInstanceId = instanceAccessService.GetBaseMapById(1).MapInstanceId,
                    DestinationX = 5,
                    DestinationY = 5,
                    PortalId = 1,
                    SourceMapId = _map.MapId,
                    SourceX = 0,
                    SourceY = 0,
                }
            };

            Broadcaster.Instance.RegisterSession(_session);
            _finsPacketHandler = new FinsPacketHandler(conf,_logger);
        }
        private void InitializeTargetSession()
        {
            var targetAccount = new AccountDto { Name = "test2", Password = "test".ToSha512() };
            _accountDao.InsertOrUpdate(ref targetAccount);

            _targetChar = new Character(null, null, null, _characterRelationDao, _characterDao, _itemInstanceDao, _accountDao, _logger, null)
            {
                CharacterId = 1,
                Name = "TestChar2",
                Slot = 1,
                AccountId = targetAccount.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            CharacterDto character = _targetChar;
            _characterDao.InsertOrUpdate(ref character);
            var instanceAccessService = new MapInstanceProvider(new List<MapDto> { _map, _map2 },
                new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                _mapNpcDao,
                _mapMonsterDao, _portalDao, new Adapter(), _logger);
            _targetSession = new ClientSession(null,
                    instanceAccessService, null, _logger, null)
                { SessionId = 2 };

            _targetSession.InitializeAccount(targetAccount);

            _targetSession.SetCharacter(_targetChar);
            _targetSession.Character.MapInstance = instanceAccessService.GetBaseMapById(0);
            _targetSession.Character.CharacterId = 2;
            Broadcaster.Instance.RegisterSession(_targetSession);
        }



        [TestMethod]
        public void Test_Add_Friend()
        {
            InitializeTargetSession();
            _targetSession.Character.FriendRequestCharacters.TryAdd(0, _session.Character.CharacterId);
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            _finsPacketHandler.Execute(finsPacket, _session);
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s =>
                    s.Value.RelatedCharacterId == _targetSession.Character.CharacterId)
                && _targetSession.Character.CharacterRelations.Any(s =>
                    s.Value.RelatedCharacterId == _session.Character.CharacterId));
        }

        [TestMethod]
        public void Test_Add_Friend_When_Disconnected()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = 2,
                Type = FinsPacketType.Accepted
            };
            _finsPacketHandler.Execute(finsPacket, _session);

            Assert.IsTrue(_session.Character.CharacterRelations.IsEmpty);
        }

        [TestMethod]
        public void Test_Add_Not_Requested_Friend()
        {
            InitializeTargetSession();
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetChar.CharacterId,
                Type = FinsPacketType.Accepted
            };
            _finsPacketHandler.Execute(finsPacket, _session);
            Assert.IsTrue(_session.Character.CharacterRelations.IsEmpty &&
                _targetSession.Character.CharacterRelations.IsEmpty);
        }

    }
}
