using System;
using System.Collections.Generic;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ClientPackets.Shops;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Shop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.PacketHandlers.Shops;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class SellPacketHandlerTests
    {
        private SellPacketHandler _sellPacketHandler;
        MapInstanceProvider _instanceProvider;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            Broadcaster.Reset();
            TestHelpers.Reset();
            _instanceProvider = TestHelpers.Instance.MapInstanceProvider;
            _session = TestHelpers.Instance.GenerateSession();
            _sellPacketHandler = new SellPacketHandler(TestHelpers.Instance.WorldConfiguration);
        }


        [TestMethod]
        public void UserCanNotSellInExchange()
        {
            _session.Character.InExchangeOrTrade = true;
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Etc, VNum = 1, IsTradable = true},
            };
            var itemBuilder = new ItemProvider(items, new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());

            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 1), PocketType.Etc, 0);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 2), PocketType.Etc, 1);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 3), PocketType.Etc, 2);

            _session.Character.MapInstance = _instanceProvider.GetBaseMapById(1);
            _sellPacketHandler.Execute(new SellPacket { Slot = 0, Amount = 1, Data = (short)PocketType.Etc }, _session);
            Assert.IsTrue(_session.Character.Gold == 0);
            Assert.IsNotNull(_session.Character.Inventory.LoadBySlotAndType<IItemInstance>(0, PocketType.Etc));
        }

        [TestMethod]
        public void UserCanNotSellNotSoldable()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Etc, VNum = 1, IsSoldable = false},
            };
            var itemBuilder = new ItemProvider(items, new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());

            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 1), PocketType.Etc, 0);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 2), PocketType.Etc, 1);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 3), PocketType.Etc, 2);

            _session.Character.MapInstance = _instanceProvider.GetBaseMapById(1);
            _sellPacketHandler.Execute(new SellPacket { Slot = 0, Amount = 1, Data = (short)PocketType.Etc }, _session);
            var packet = (SMemoPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message ==
                Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_SOLDABLE, _session.Account.Language));
            Assert.IsTrue(_session.Character.Gold == 0);
            Assert.IsNotNull(_session.Character.Inventory.LoadBySlotAndType<IItemInstance>(0, PocketType.Etc));
        }

        [TestMethod]
        public void UserCanSell()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Etc, VNum = 1, IsSoldable = true, Price = 500000},
            };
            var itemBuilder = new ItemProvider(items, new List<IEventHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());

            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 1), PocketType.Etc, 0);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 2), PocketType.Etc, 1);
            _session.Character.Inventory.AddItemToPocket(itemBuilder.Create(1, 3), PocketType.Etc, 2);

            _session.Character.MapInstance = _instanceProvider.GetBaseMapById(1);
            _sellPacketHandler.Execute(new SellPacket { Slot = 0, Amount = 1, Data = (short)PocketType.Etc }, _session);
            Assert.IsTrue(_session.Character.Gold > 0);
            Assert.IsNull(_session.Character.Inventory.LoadBySlotAndType<IItemInstance>(0, PocketType.Etc));
        }
    }
}
