//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared.AutoFixture;
using Serilog;

namespace NosCore.Tests.Shared.BDD
{
    public abstract class SpecBase
    {
        protected NosCoreFixture Fixture { get; private set; } = null!;
        protected ClientSession Session { get; set; } = null!;
        protected ItemGenerationService ItemProvider { get; set; } = null!;
        protected ILogger Logger { get; } = new Mock<ILogger>().Object;

        protected List<ItemDto> DefaultItems { get; } = new()
        {
            new Item { Type = NoscorePocketType.Main, VNum = 1012, IsSoldable = true, IsDroppable = true },
            new Item { Type = NoscorePocketType.Main, VNum = 1013 },
            new Item { Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon },
            new Item { Type = NoscorePocketType.Equipment, VNum = 2, ItemType = ItemType.Weapon },
            new Item { Type = NoscorePocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist },
            new Item { Type = NoscorePocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion }
        };

        [TestInitialize]
        public virtual async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            Fixture = new NosCoreFixture();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.StaticBonusList = new List<StaticBonusDto>();
            ItemProvider = new ItemGenerationService(
                DefaultItems,
                new EventLoaderService<Item, System.Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                    new List<IEventHandler<Item, System.Tuple<InventoryItemInstance, UseItemPacket>>>()),
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        protected void CharacterHasGold(long gold) => Session.Character.Gold = gold;

        protected void CharacterHasItem(short vnum) => CharacterHasItem(vnum, 1);

        protected void CharacterHasItem(short vnum, short amount)
        {
            var item = ItemProvider.Create(vnum, amount);
            var pocketType = item.Item.Type;
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, 0), pocketType, 0);
        }

        protected void CharacterHasMedalBonus(StaticBonusType bonusType)
        {
            Session.Character.StaticBonusList.Add(new StaticBonusDto { StaticBonusType = bonusType });
        }

        protected void CharacterIsInShop() => Session.Character.InShop = true;

        protected void InventoryShouldBeEmpty() =>
            Assert.AreEqual(0, Session.Character.InventoryService.Count);

        protected void InventoryShouldHaveCount(int count) =>
            Assert.AreEqual(count, Session.Character.InventoryService.Count);

        protected void InventoryShouldContainItem(short vnum, short amount)
        {
            var item = Session.Character.InventoryService.FirstOrDefault();
            Assert.IsNotNull(item.Value);
            Assert.AreEqual(vnum, item.Value.ItemInstance.ItemVNum);
            Assert.AreEqual(amount, item.Value.ItemInstance.Amount);
        }

        protected void GoldShouldBe(long expected) =>
            Assert.AreEqual(expected, Session.Character.Gold);

        protected void NoPacketShouldBeSent() =>
            Assert.IsNull(Session.LastPackets.FirstOrDefault());

        protected T? GetLastPacket<T>() where T : class, IPacket =>
            (T?)Session.LastPackets.FirstOrDefault(s => s is T);

        protected void ShouldReceiveMessage(Game18NConstString message)
        {
            var packet = GetLastPacket<MsgiPacket>();
            Assert.IsNotNull(packet);
            Assert.AreEqual(message, packet.Message);
        }

        protected void ShouldReceiveMessage(Game18NConstString message, MessageType messageType)
        {
            var packet = GetLastPacket<MsgiPacket>();
            Assert.IsNotNull(packet);
            Assert.AreEqual(message, packet.Message);
            Assert.AreEqual(messageType, packet.Type);
        }

        protected void ShouldReceiveModalMessage(Game18NConstString message)
        {
            var packet = GetLastPacket<ModaliPacket>();
            Assert.IsNotNull(packet);
            Assert.AreEqual(message, packet.Message);
        }

        protected void ShouldReceiveModalMessage(Game18NConstString message, byte type, byte argumentType)
        {
            var packet = GetLastPacket<ModaliPacket>();
            Assert.IsNotNull(packet);
            Assert.AreEqual(message, packet.Message);
            Assert.AreEqual(type, packet.Type);
            Assert.AreEqual(argumentType, packet.ArgumentType);
        }
    }
}
