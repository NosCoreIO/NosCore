using System;
using System.Collections.Generic;
using System.Text;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.ClientPackets.Movement;
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
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.PacketHandlers.Login;
using NosCore.PacketHandlers.Movement;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class PreqPacketHandlerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IGenericDao<AccountDto> _accountDao = new GenericDao<Database.Entities.Account, AccountDto>(_logger);
        private readonly IGenericDao<PortalDto> _portalDao = new GenericDao<Database.Entities.Portal, PortalDto>(_logger);
        private readonly IGenericDao<MapMonsterDto> _mapMonsterDao = new GenericDao<Database.Entities.MapMonster, MapMonsterDto>(_logger);
        private readonly IGenericDao<MapNpcDto> _mapNpcDao = new GenericDao<Database.Entities.MapNpc, MapNpcDto>(_logger);
        private readonly IGenericDao<MapDto> _mapDao = new GenericDao<Database.Entities.Map, MapDto>(_logger);
        private const string Name = "TestExistingCharacter";
        private ClientSession _session;
        private PreqPacketHandler _preqPacketHandler;
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
        [TestInitialize]
        public void Setup()
        {
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto { MapId = 1 };
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig()
                .ConstructUsing(src => new MapNpc(new ItemProvider(new List<ItemDto>(), null), null, null, new List<NpcMonsterDto>(), _logger));
            TypeAdapterConfig<MapMonsterDto, MapMonster>.NewConfig()
                .ConstructUsing(src => new MapMonster(new List<NpcMonsterDto>(), _logger));
            new Mapper();
            _mapDao.InsertOrUpdate(ref map);
            var _acc = new AccountDto { Name = Name, Password = "test".ToSha512() };
            _accountDao.InsertOrUpdate(ref _acc);
            _instanceProvider = new MapInstanceProvider(new List<MapDto> { _map, _mapShop },
                new MapItemProvider(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                _mapNpcDao,
                _mapMonsterDao, _portalDao, new Adapter(), _logger);
            _instanceProvider.Initialize();
            _session = new ClientSession(new WorldConfiguration(), _instanceProvider, null, _logger, new List<IPacketHandler>());
            _session.InitializeAccount(_acc);

            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues = new Dictionary<WebApiRoute, object>();
            _session.SetCharacter(new Character(null, null, null, null, null, null, null, _logger, null)
            {
                CharacterId = 1,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = _acc.AccountId,
                MapId = 1,
                State = CharacterState.Active
            });
            var channelMock = new Mock<IChannel>();
            _session.RegisterChannel(channelMock.Object);
            _preqPacketHandler = new PreqPacketHandler(_instanceProvider);
            _session.Character.MapInstance = _instanceProvider.GetBaseMapById(0);
            _session.Character.MapInstance.Portals = new List<Portal> { new Portal { DestinationMapId = 1, DestinationMapInstanceId = _instanceProvider.GetBaseMapInstanceIdByMapId(1),
                DestinationX = 5, DestinationY = 5, SourceMapId = 0, SourceMapInstanceId = _instanceProvider.GetBaseMapInstanceIdByMapId(0), SourceX = 0, SourceY = 0 } };
        }

        [TestMethod]
        public void UserCanUsePortal()
        {
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _preqPacketHandler.Execute(new PreqPacket(), _session);
            Assert.IsTrue(_session.Character.PositionY == 5 && _session.Character.PositionX == 5 &&
                _session.Character.MapInstance.Map.MapId == 1);
        }

        [TestMethod]
        public void UserCanTUsePortalIfTooFar()
        {
            _session.Character.PositionX = 8;
            _session.Character.PositionY = 8;
            _preqPacketHandler.Execute(new PreqPacket(), _session);
            Assert.IsTrue(_session.Character.PositionY == 8 && _session.Character.PositionX == 8 &&
                _session.Character.MapInstance.Map.MapId == 0);
        }
    }
}
