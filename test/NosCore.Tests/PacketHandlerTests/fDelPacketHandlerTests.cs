using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.ClientPackets.Movement;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.ClientPackets.Shops;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Login;
using ChickenAPI.Packets.ServerPackets.Shop;
using ChickenAPI.Packets.ServerPackets.UI;
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
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.GameObject.Providers.MapItemProvider.Handlers;
using NosCore.PacketHandlers.Friend;
using NosCore.PacketHandlers.Game;
using NosCore.PacketHandlers.Login;
using NosCore.PacketHandlers.Shops;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class FDelPacketHandlerTests
    {
        private FdelPacketHandler _fDelPacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IGenericDao<MapDto> _mapDao = new GenericDao<Database.Entities.Map, MapDto>(_logger);

        private readonly IGenericDao<AccountDto> _accountDao =
            new GenericDao<Database.Entities.Account, AccountDto>(_logger);

        private readonly IGenericDao<CharacterDto> _characterDao =
            new GenericDao<Database.Entities.Character, CharacterDto>(_logger);

        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao =
            new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>(_logger);

        private readonly IGenericDao<PortalDto> _portalDao =
            new GenericDao<Database.Entities.Portal, PortalDto>(_logger);

        private readonly IGenericDao<MapMonsterDto> _mapMonsterDao =
            new GenericDao<Database.Entities.MapMonster, MapMonsterDto>(_logger);

        private readonly IGenericDao<MapNpcDto> _mapNpcDao =
            new GenericDao<Database.Entities.MapNpc, MapNpcDto>(_logger);

        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao = new ItemInstanceDao(_logger);

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

        private readonly Map _mapShop = new Map
        {
            MapId = 1,
            Name = "shopMap",
            ShopAllowed = true,
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

        MapInstanceProvider _instanceProvider;
        private ClientSession _session;
        private ClientSession _targetSession;
        private Character _targetChar;

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IInitializable>()
                .AfterMapping(dest => Task.Run(() => dest.Initialize()));
            Broadcaster.Reset();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto {MapId = 1};
            _mapDao.InsertOrUpdate(ref map);
            var account = new AccountDto {Name = "AccountTest", Password = "test".ToSha512()};
            _accountDao.InsertOrUpdate(ref account);
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues =
                new Dictionary<WebApiRoute, object>
                {
                    {WebApiRoute.Channel, new List<ChannelInfo> {new ChannelInfo()}},
                    {WebApiRoute.ConnectedAccount, new List<ConnectedAccount>()}
                };

            var conf = new WorldConfiguration {BackpackSize = 3, MaxItemAmount = 999, MaxGoldAmount = 999_999_999};
            var _chara = new Character(new InventoryService(new List<ItemDto>(), conf, _logger), null, null,
                _characterRelationDao, _characterDao, _itemInstanceDao, _accountDao, _logger, null)
            {
                CharacterId = 1,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = account.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            _instanceProvider = new MapInstanceProvider(new List<MapDto> {_map, _mapShop},
                new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                _mapNpcDao,
                _mapMonsterDao, _portalDao, new Adapter(), _logger);
            _instanceProvider.Initialize();
            var channelMock = new Mock<IChannel>();
            _session = new ClientSession(null, _logger, null);
            _session.RegisterChannel(channelMock.Object);
            _session.InitializeAccount(account);
            _session.SessionId = 1;
            _session.SetCharacter(_chara);
            var mapinstance = _instanceProvider.GetBaseMapById(0);
            _session.Character.Account = account;
            _session.Character.MapInstance = mapinstance;
            _session.Character.MapInstance.Portals = new List<Portal>
            {
                new Portal
                {
                    DestinationMapId = _map.MapId,
                    Type = PortalType.Open,
                    SourceMapInstanceId = mapinstance.MapInstanceId,
                    DestinationMapInstanceId = mapinstance.MapInstanceId,
                    DestinationX = 5,
                    DestinationY = 5,
                    PortalId = 1,
                    SourceMapId = _map.MapId,
                    SourceX = 0,
                    SourceY = 0,
                }
            };

            Broadcaster.Instance.RegisterSession(_session);
            _fDelPacketHandler = new FdelPacketHandler();
        }

        private void InitializeTargetSession()
        {
            var targetAccount = new AccountDto {Name = "test2", Password = "test".ToSha512()};
            _accountDao.InsertOrUpdate(ref targetAccount);

            _targetChar = new Character(null, null, null, _characterRelationDao, _characterDao, _itemInstanceDao,
                _accountDao, _logger, null)
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
            var instanceAccessService = new MapInstanceProvider(new List<MapDto> {_map},
                new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                _mapNpcDao,
                _mapMonsterDao, _portalDao, new Adapter(), _logger);
            _targetSession = new ClientSession(null,
                    instanceAccessService, null, _logger, null)
                {SessionId = 2};

            _targetSession.InitializeAccount(targetAccount);

            _targetSession.SetCharacter(_targetChar);
            _targetSession.Character.MapInstance = instanceAccessService.GetBaseMapById(0);
            _targetSession.Character.CharacterId = 2;
            Broadcaster.Instance.RegisterSession(_targetSession);
        }


        [TestMethod]
        public void Test_Delete_Friend_When_Disconnected()
        {
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            _session.Character.CharacterRelations.TryAdd(guid,
                new CharacterRelation
                {
                    CharacterId = _session.Character.CharacterId,
                    CharacterRelationId = guid,
                    RelatedCharacterId = 2,
                    RelationType = CharacterRelationType.Friend
                });
            _session.Character.RelationWithCharacter.TryAdd(targetGuid,
                new CharacterRelation
                {
                    CharacterId = 2,
                    CharacterRelationId = targetGuid,
                    RelatedCharacterId = _session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                });

            Assert.IsTrue(_session.Character.CharacterRelations.Count == 1 &&
                _session.Character.RelationWithCharacter.Count == 1);

            var fdelPacket = new FdelPacket
            {
                CharacterId = 2
            };

            _fDelPacketHandler.Execute(fdelPacket, _session);

            Assert.IsTrue(_session.Character.CharacterRelations.IsEmpty);
        }


        [TestMethod]
        public void Test_Delete_Friend()
        {
            InitializeTargetSession();
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
            new FinsPacketHandler(new WorldConfiguration(), _logger).Execute(finsPacket, _session);
            _fDelPacketHandler.Execute(fdelPacket, _session);
            Assert.IsTrue(_session.Character.CharacterRelations.All(s =>
                    s.Value.RelatedCharacterId != _targetSession.Character.CharacterId)
                && _targetSession.Character.CharacterRelations.All(s =>
                    s.Value.RelatedCharacterId != _session.Character.CharacterId));
        }
    }
}