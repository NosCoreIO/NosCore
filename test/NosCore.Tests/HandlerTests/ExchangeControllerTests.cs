using System;
using System.Collections.Generic;
using System.Text;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core;
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
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.Inventory;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.GameObject.Services.MapItemBuilder;
using NosCore.GameObject.Services.MapItemBuilder.Handlers;
using NosCore.GameObject.Services.MapMonsterBuilder;
using NosCore.GameObject.Services.MapNpcBuilder;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.Enumerations.Map;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class ExchangeControllerTests
    {
        private readonly WorldConfiguration _worldConfiguration = new WorldConfiguration
        {
            BackpackSize = 48,
            MaxBankGoldAmount = 100000000000,
            MaxGoldAmount = 1000000000
        };

        private ItemBuilderService _itemBuilderService;

        private ExchangeService _exchangeService;

        private ExchangePacketController _handler;

        private ExchangePacketController _handler2;

        private ExchangePacketController _handler3;

        private ClientSession _session;

        private ClientSession _session2;

        private ClientSession _session3;

        private MapInstance _map;

        [TestInitialize]
        public void Setup()
        {
            SystemTime.Freeze();
            PacketFactory.Initialize<NoS0575Packet>();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);

            var items = new List<Item>
            {
                new Item { Type = PocketType.Main, VNum = 1012 },
                new Item { Type = PocketType.Main, VNum = 1013 },
            };

            _itemBuilderService = new ItemBuilderService(items, new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());
            _exchangeService = new ExchangeService(_itemBuilderService, _worldConfiguration);

            _session = new ClientSession(_worldConfiguration,
                new List<PacketController> { new ExchangePacketController() }, null, _exchangeService);

            _session2 = new ClientSession(_worldConfiguration, new List<PacketController> { new ExchangePacketController() }, null, _exchangeService);

            _session3 = new ClientSession(_worldConfiguration, new List<PacketController> { new ExchangePacketController() }, null, _exchangeService);

            var account1 = new AccountDto { Name = "AccountTest", Password = "test".ToSha512() };
            var character1 = new CharacterDto
            {
                CharacterId = 1,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = account1.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            var account2 = new AccountDto { Name = "Account2", Password = "test".ToSha512() };
            var character2 = new CharacterDto
            {
                CharacterId = 2,
                Name = "TestExistingCharacter2",
                Slot = 1,
                AccountId = account2.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            var account3 = new AccountDto { Name = "Account3", Password = "test".ToSha512() };
            var character3 = new CharacterDto
            {
                CharacterId = 3,
                Name = "TestExistingCharacter3",
                Slot = 1,
                AccountId = account3.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            _session.InitializeAccount(account1);
            _session2.InitializeAccount(account2);
            _session3.InitializeAccount(account3);
            
            _itemBuilderService = new ItemBuilderService(items, new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());
            _handler = new ExchangePacketController(_worldConfiguration, _exchangeService);
            _handler2 = new ExchangePacketController(_worldConfiguration, _exchangeService);
            _handler3 = new ExchangePacketController(_worldConfiguration, _exchangeService);
            _map = new MapInstance(new Map
            {
                Name = "testMap",
                Data = new byte[]
                {
                    8, 0, 8, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0
                }
            }
            , Guid.NewGuid(), false, MapInstanceType.BaseMapInstance, new List<NpcMonsterDto>(), null, null, null);
            _handler.RegisterSession(_session);
            _handler2.RegisterSession(_session2);
            _handler3.RegisterSession(_session3);

            _session.SetCharacter(character1.Adapt<Character>());
            _session2.SetCharacter(character2.Adapt<Character>());
            _session3.SetCharacter(character3.Adapt<Character>());

            _session.Character.MapInstance = _map;
            _session2.Character.MapInstance = _map;
            _session3.Character.MapInstance = _map;

            _session.Character.Account = account1;
            _session2.Character.Account = account2;
            _session3.Character.Account = account3;

            _session.Character.Inventory = new InventoryService(items, _worldConfiguration);
            _session2.Character.Inventory = new InventoryService(items, _worldConfiguration);
            _session3.Character.Inventory = new InventoryService(items, _worldConfiguration);

            _session.Character.ExchangeService = _exchangeService;
            _session2.Character.ExchangeService = _exchangeService;
            _session3.Character.ExchangeService = _exchangeService;
            _session2.SessionId = 1;
            _session3.SessionId = 2;
            Broadcaster.Instance.RegisterSession(_session);
            Broadcaster.Instance.RegisterSession(_session2);
            Broadcaster.Instance.RegisterSession(_session3);
        }

        [TestMethod]
        public void Test_Open_Exchange()
        {
            var requestPacket = new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.List,
                VisualId = _session2.Character.CharacterId
            };

            _handler.RequestExchange(requestPacket);
            Assert.IsTrue(_exchangeService.CheckExchange(_session.Character.VisualId) && _exchangeService.CheckExchange(_session2.Character.CharacterId));
        }

        [TestMethod]
        public void Test_Open_Second_Exchange()
        {
            var requestPacket = new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.List,
                VisualId = _session2.Character.VisualId
            };

            _handler.RequestExchange(requestPacket);
            Assert.IsTrue(_exchangeService.CheckExchange(_session.Character.VisualId) && _exchangeService.CheckExchange(_session2.Character.CharacterId));

            requestPacket.VisualId = _session3.Character.VisualId;

            _handler.RequestExchange(requestPacket);
            Assert.IsFalse(_exchangeService.CheckExchange(_session3.Character.CharacterId));
        }
        
        [TestMethod]
        public void Test_Exchange_User_Blocked()
        {
            var guid = Guid.NewGuid();

            var characterRelation = new CharacterRelation
            {
                CharacterId = _session.Character.CharacterId,
                CharacterRelationId = guid,
                RelatedCharacterId = _session2.Character.CharacterId,
                RelationType = CharacterRelationType.Blocked
            };

            _session.Character.CharacterRelations.TryAdd(guid, characterRelation);

            var requestPacket = new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.List,
                VisualId = _session2.Character.VisualId
            };

            _handler.RequestExchange(requestPacket);
            Assert.IsFalse(_exchangeService.CheckExchange(_session.Character.VisualId) && _exchangeService.CheckExchange(_session2.Character.CharacterId));
        }

        //TODO: Test Exchange List (Need to fix packet factory first)
        //TODO: Test invalid exchange list
        //TODO: Test decline exchange
        //TODO: Test cancel exchange
        //TODO: Test cancel when not in exchange
        //TODO: Test actual exchange
        //TODO: Test invalid exchange (not existing items, not tradable items)
        //TODO: Test gold exchange
        //TODO: Test invalid gold exchange
        //TODO: Test exchange with full inventory on one character
        //TODO: Test exchange will full inventory on both characters
        //TODO: Test exchange when target has blocked exchange requests
        //TODO: Test cancel exchange when someone has set exchanges in trade
        //TODO: Test character disconnection in exchange
    }
}
