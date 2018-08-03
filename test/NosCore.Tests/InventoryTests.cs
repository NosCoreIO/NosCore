using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Core.Serializing;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Item;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.Tests
{
    [TestClass]
    public class InventoryTests
    {
        private Inventory Inventory { get; set; }

        [TestInitialize]
        public void Setup()
        {
            ServerManager.Instance.Items = new List<GameObject.Item.Item>()
            {
                new Item(){Type = PocketType.Main, VNum = 1012,  },
                new Item(){Type = PocketType.Main, VNum = 1013,  }
            };
            Inventory = new Inventory
            {
                Configuration = new WorldConfiguration() { BackpackSize = 3, MaxItemAmount = 999 }
            };
        }

        #region AddItemToPocket
        //TODO change all tests to use AddItemToPocket
        [TestMethod]
        public void CreateItem()
        {
            var item = Inventory.AddItemToPocket(ItemInstance.Create(1012, 0)).First();
            Assert.IsTrue(item.Amount == 1 && item.ItemVNum == 1012 && item.Type == PocketType.Main);
        }

        [TestMethod]
        public void CreateItemAndStackIt()
        {
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0));
            var item = Inventory.AddItemToPocket(ItemInstance.Create(1012, 0)).First();
            Assert.IsTrue(item.Amount == 2 && item.ItemVNum == 1012);
        }

        [TestMethod]
        public void CreateItemWhenSlotMax()
        {
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 999));
            var items = Inventory.AddItemToPocket(ItemInstance.Create(1012, 0));
            Assert.IsTrue(items.First().Amount == 1);
        }

        [TestMethod]
        public void CreateItemWhenSlotFilled()
        {
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 990));
            var items = Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 29));
            Assert.IsTrue(items.First().Amount == 999 && items.Last().Amount == 20);
        }

        [TestMethod]
        public void CreateItemAndFillMultiSlot()
        {
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 990), slot: 0);
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 990), slot: 1);
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 990), slot: 2);
            var items = Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 27));
            Assert.IsTrue(items.All(item => item.Amount == 999) && items.Count == 3);
        }

        [TestMethod]
        public void CreateMoreItemThanInventoryPlace()
        {
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 990), slot: 0);
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 990), slot: 1);
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 990), slot: 2);
            var items = Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 99));
            Assert.IsTrue(Inventory.Values.All(item => item.Amount == 990) && items.Count == 0);
        }

        [TestMethod]
        public void CreateStackOnASpecificItem()
        {
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 990));
            Inventory.AddItemToPocket(ItemInstance.Create(1013, 0, 990));
            Inventory.AddItemToPocket(ItemInstance.Create(1013, 0));
            Assert.IsTrue(Inventory.Values.First(item => item.Slot == 1).Amount == 991);
        }

        [TestMethod]
        public void CreateDoesntStackOnWrontItem()
        {
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 990));
            Inventory.AddItemToPocket(ItemInstance.Create(1013, 0, 990));
            Inventory.AddItemToPocket(ItemInstance.Create(1013, 0, 19));
            Assert.IsTrue(Inventory.Values.First(item => item.Slot == 0).Amount == 990);
        }
        #endregion

        #region LoadBySlotAndType  
        [TestMethod]
        public void LoadItemOnAnNotEmptySlot()
        {
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 990));
            var item = Inventory.LoadBySlotAndType<ItemInstance>(0, PocketType.Main);
            Assert.IsTrue(item.ItemVNum == 1012 && item.Amount == 990);
        }

        [TestMethod]
        public void LoadAnNonExistingItem()
        {
            Inventory.AddItemToPocket(ItemInstance.Create(1012, 0, 990));
            var item = Inventory.LoadBySlotAndType<ItemInstance>(1, PocketType.Main);
            Assert.IsNull(item);
        }
        #endregion
    }
}
