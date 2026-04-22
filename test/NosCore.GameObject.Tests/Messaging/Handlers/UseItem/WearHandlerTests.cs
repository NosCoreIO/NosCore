//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class WearHandlerTests
    {
        private const short ArmorVNum = 1;
        private ClientSession _session = null!;
        private WearHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _handler = new WearHandler(
                new Mock<ILogger>().Object,
                TestHelpers.Instance.Clock,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.WorldConfiguration);
        }

        [TestMethod]
        public async Task NonWearableItemTypeIsIgnored()
        {
            await new Spec("Wear handler returns early for item types that are not Weapon/Jewelery/Armor/Fashion/Specialist")
                .Given(ItemInInventoryOfType_, ItemType.Main)
                .WhenAsync(UsingTheItem)
                .Then(NoCanNotWearPacketSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LevelTooLowIsRejectedWithCanNotWearThat()
        {
            await new Spec("An item whose LevelMinimum exceeds character.Level is rejected with CanNotWearThat")
                .Given(ItemInInventoryOfType_, ItemType.Armor)
                .And(ItemRequiresLevel_, (byte)80)
                .And(CharacterIsLevel_, (byte)20)
                .WhenAsync(UsingTheItem)
                .Then(CanNotWearThatShouldHaveBeenSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MismatchingClassIsRejectedWithCanNotWearThat()
        {
            await new Spec("An item whose Class bitmask doesn't include the character's Class is rejected")
                .Given(ItemInInventoryOfType_, ItemType.Armor)
                .And(ItemIsRestrictedToClassBit_, (byte)4)
                .And(CharacterIsClass_, CharacterClassType.Adventurer)
                .WhenAsync(UsingTheItem)
                .Then(CanNotWearThatShouldHaveBeenSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MismatchingGenderIsRejectedWithCanNotWearThat()
        {
            await new Spec("An item whose Sex bitmask doesn't include the character's Gender is rejected (male-only item on a Female character)")
                .Given(ItemInInventoryOfType_, ItemType.Fashion)
                .And(ItemIsRestrictedToSexBit_, (byte)1)
                .And(CharacterGenderIs_, GenderType.Female)
                .WhenAsync(UsingTheItem)
                .Then(CanNotWearThatShouldHaveBeenSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequireBindingModeZeroUnboundEmitsConfirmationDialog()
        {
            await new Spec("RequireBinding + Mode=0 + not yet bound prompts a Qna dialog asking for consent instead of equipping")
                .Given(ItemInInventoryOfType_, ItemType.Armor)
                .And(ItemRequiresBinding)
                .WhenAsync(UsingTheItemWithMode_, (byte)0)
                .Then(QnaPacketShouldHaveBeenSent)
                .And(NoCanNotWearPacketSent)
                .ExecuteAsync();
        }

        private void ItemInInventoryOfType_(ItemType itemType)
        {
            var item = new Item
            {
                VNum = ArmorVNum,
                Type = NoscorePocketType.Equipment,
                ItemType = itemType,
                EquipmentSlot = EquipmentType.Armor,
            };
            var inst = new ItemInstanceForTest(ArmorVNum) { Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void ItemRequiresLevel_(byte level)
        {
            _item.ItemInstance.Item.LevelMinimum = level;
        }

        private void ItemIsRestrictedToClassBit_(byte classBit)
        {
            _item.ItemInstance.Item.Class = classBit;
        }

        private void ItemIsRestrictedToSexBit_(byte sexBit)
        {
            _item.ItemInstance.Item.Sex = sexBit;
        }

        private void ItemRequiresBinding()
        {
            _item.ItemInstance.Item.RequireBinding = true;
            _item.ItemInstance.BoundCharacterId = null;
        }

        private void CharacterIsLevel_(byte level)
        {
            _session.Character.Level = level;
        }

        private void CharacterIsClass_(CharacterClassType cls)
        {
            _session.Character.Class = cls;
        }

        private void CharacterGenderIs_(GenderType gender)
        {
            _session.Character.Gender = gender;
        }

        private async Task UsingTheItem() => await UsingTheItemWithMode_(1);

        private async Task UsingTheItemWithMode_(byte mode)
        {
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Equipment,
                Slot = 0,
                Mode = mode,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _item, packet));
        }

        private void CanNotWearThatShouldHaveBeenSent()
        {
            var say = _session.LastPackets.OfType<SayiPacket>()
                .FirstOrDefault(p => p.Message == Game18NConstString.CanNotWearThat);
            Assert.IsNotNull(say);
        }

        private void NoCanNotWearPacketSent()
        {
            var say = _session.LastPackets.OfType<SayiPacket>()
                .FirstOrDefault(p => p.Message == Game18NConstString.CanNotWearThat);
            Assert.IsNull(say);
        }

        private void QnaPacketShouldHaveBeenSent()
        {
            Assert.IsTrue(_session.LastPackets.OfType<QnaPacket>().Any());
        }

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Equipment };
            public object Clone() => MemberwiseClone();
        }
    }
}
