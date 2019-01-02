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
using NosCore.GameObject;
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
using NosCore.Configuration;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.MapItemBuilder;
using NosCore.Shared;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.GameObject.Services.MapMonsterBuilder;
using NosCore.GameObject.Services.MapNpcBuilder;
using NosCore.GameObject.Services.NRunAccess;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Group;
using NosCore.Shared.I18N;
using NosCore.Shared.Enumerations.Items;
using NosCore.GameObject.Services.Inventory;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class NpcPacketControllerTests
    {
        private ClientSession _session;
        private NpcPacketController _handler;

        MapInstanceAccessService _instanceAccessService;

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

        private MShopPacket shopPacket = new MShopPacket
        {
            Type = CreateShopPacketType.Open,
            ItemList = new List<MShopItemSubPacket>
            {
                new MShopItemSubPacket {Type = PocketType.Etc, Slot = 0, Amount = 1, Price = 10000},
                new MShopItemSubPacket {Type = PocketType.Etc, Slot = 1, Amount = 2, Price = 20000},
                new MShopItemSubPacket {Type = PocketType.Etc, Slot = 2, Amount = 3, Price = 30000},
            },
            Name = "TEST SHOP"
        };

        private ItemBuilderService _itemBuilderService;

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            Broadcaster.Reset();
            var contextBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto { MapId = 1 };
            DaoFactory.MapDao.InsertOrUpdate(ref map);
            var account = new AccountDto { Name = "AccountTest", Password = "test".ToSha512() };
            DaoFactory.AccountDao.InsertOrUpdate(ref account);
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues =
                new Dictionary<WebApiRoute, object>
                {
                    { WebApiRoute.Channel, new List<ChannelInfo> { new ChannelInfo() } },
                    { WebApiRoute.ConnectedAccount, new List<ConnectedAccount>() }
                };

            var _chara = new CharacterDto
            {
                CharacterId = 1,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = account.AccountId,
                MapId = 1,
                State = CharacterState.Active
            };

            DaoFactory.CharacterDao.InsertOrUpdate(ref _chara);

            _itemBuilderService = new ItemBuilderService(new List<Item>(),
               new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());
            _instanceAccessService = new MapInstanceAccessService(new List<NpcMonsterDto>(), new List<Map> { _map, _mapShop },
                new MapItemBuilderService(new List<IHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                new MapNpcBuilderService(_itemBuilderService, new List<ShopDto>(), new List<ShopItemDto>(), new List<NpcMonsterDto>(), new List<MapNpcDto>()),
                new MapMonsterBuilderService(new List<Item>(), new List<ShopDto>(), new List<ShopItemDto>(), new List<NpcMonsterDto>(), new List<MapMonsterDto>()));

            var channelMock = new Mock<IChannel>();
            _session = new ClientSession(null, new List<PacketController> { new DefaultPacketController(null, _instanceAccessService, null) }, _instanceAccessService);
            _session.RegisterChannel(channelMock.Object);
            _session.InitializeAccount(account);
            _session.SessionId = 1;
            var conf = new WorldConfiguration(){BackpackSize = 3, MaxItemAmount = 999, MaxGoldAmount = 999_999_999};
            _handler = new NpcPacketController(conf, new NrunAccessService(new List<IHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>>()));
            _handler.RegisterSession(_session);
            _session.SetCharacter(_chara.Adapt<Character>());
            var mapinstance = _instanceAccessService.GetBaseMapById(0);

            _session.Character.MapInstance = _instanceAccessService.GetBaseMapById(0);
            _session.Character.MapInstance = mapinstance;
            _session.Character.MapInstance.Portals = new List<Portal> { new Portal
            {
                DestinationMapId =_map.MapId,
                Type = PortalType.Open,
                SourceMapInstanceId = mapinstance.MapInstanceId,
                DestinationMapInstanceId = _instanceAccessService.GetBaseMapById(0).MapInstanceId,
                DestinationX = 5,
                DestinationY = 5,
                PortalId = 1,
                SourceMapId = _map.MapId,
                SourceX = 0,
                SourceY = 0,
            } };

            _session.Character.Inventory = new InventoryService(new List<Item>(), conf);
            Broadcaster.Instance.RegisterSession(_session);
        }

        [TestMethod]
        public void UserCanNotCreateShopCloseToPortal()
        {
            _handler.CreateShop(shopPacket);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.SHOP_NEAR_PORTAL, _session.Account.Language));
            Assert.IsNull(_session.Character.Shop);
        }

        [TestMethod]
        public void UserCanNotCreateShopInTeam()
        {
            _session.Character.PositionX = 7;
            _session.Character.PositionY = 7;
            _session.Character.Group = new Group(GroupType.Team);
            _handler.CreateShop(shopPacket);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.SHOP_NOT_ALLOWED_IN_RAID, _session.Account.Language));
            Assert.IsNull(_session.Character.Shop);
        }

        [TestMethod]
        public void UserCanCreateShopInGroup()
        {
            _session.Character.PositionX = 7;
            _session.Character.PositionY = 7;
            _session.Character.Group = new Group(GroupType.Group);
            _handler.CreateShop(shopPacket);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message != Language.Instance.GetMessageFromKey(LanguageKey.SHOP_NOT_ALLOWED_IN_RAID, _session.Account.Language));
        }

        [TestMethod]
        public void UserCanNotCreateShopInNotShopAllowedMaps()
        {
            _session.Character.PositionX = 7;
            _session.Character.PositionY = 7;
            _handler.CreateShop(shopPacket);
            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.SHOP_NOT_ALLOWED, _session.Account.Language));
            Assert.IsNull(_session.Character.Shop);
        }


        [TestMethod]
        public void UserCanNotCreateShopWithMissingItem()
        {
            var items = new List<Item>
            {
                new Item {Type = PocketType.Etc, VNum = 1},
            };
            var itemBuilder = new ItemBuilderService(items, new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());

            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 1));
            _session.Character.MapInstance = _instanceAccessService.GetBaseMapById(1);
            _handler.CreateShop(shopPacket);
            Assert.IsNull(_session.Character.Shop);
        }


        [TestMethod]
        public void UserCanNotCreateShopWithMissingAmountItem()
        {
            var items = new List<Item>
            {
                new Item {Type = PocketType.Etc, VNum = 1},
            };
            var itemBuilder = new ItemBuilderService(items, new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());

            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 1), PocketType.Etc, 0);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 1), PocketType.Etc, 1);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 1), PocketType.Etc, 2);

            _session.Character.MapInstance = _instanceAccessService.GetBaseMapById(1);
            _handler.CreateShop(shopPacket);
            Assert.IsNull(_session.Character.Shop);
            var packet = (SayPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.SHOP_ONLY_TRADABLE_ITEMS, _session.Account.Language));
        }

        [TestMethod]
        public void UserCanCreateShop()
        {
            var items = new List<Item>
            {
                new Item {Type = PocketType.Etc, VNum = 1, IsTradable = true},
            };
            var itemBuilder = new ItemBuilderService(items, new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());

            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 1), PocketType.Etc, 0);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 2), PocketType.Etc, 1);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 3), PocketType.Etc, 2);

            _session.Character.MapInstance = _instanceAccessService.GetBaseMapById(1);
            _handler.CreateShop(shopPacket);
            Assert.IsNotNull(_session.Character.Shop);
        }

        [TestMethod]
        public void UserCanNotCreateEmptyShop()
        {
            _session.Character.MapInstance = _instanceAccessService.GetBaseMapById(1);
            _handler.CreateShop(new MShopPacket
            {
                Type = CreateShopPacketType.Open,
                ItemList = new List<MShopItemSubPacket>(),
                Name = "TEST SHOP"
            });
            Assert.IsNull(_session.Character.Shop);
            var packet = (SayPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.SHOP_EMPTY, _session.Account.Language));
        }

        //sellshop
        //buyshop

    }
}
