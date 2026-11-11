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
using Microsoft.Extensions.Logging;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class WearHandlerTests
    {
        private TestWorld World;
        private const short ArmorVNum = 1;
        private ClientSession _session;
        private WearHandler _handler;
        private InventoryItemInstance _item;

        private Mock<Wolverine.IMessageBus> _messageBus;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(1);
            _handler = new WearHandler(
                World.Clock,
                World.WorldConfiguration,
                new Mock<NosCore.GameObject.Services.BattleService.IVitalityService>().Object,
                new Mock<NosCore.GameObject.Services.BattleService.IBattleStatsProvider>().Object,
                (_messageBus = new Mock<Wolverine.IMessageBus>()).Object);
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
            await new Spec("RequireBinding + Mode=0 + not yet bound prompts the client's cannot-trade-after-wearing question instead of equipping")
                .Given(ItemInInventoryOfType_, ItemType.Armor)
                .And(ItemRequiresBinding)
                .WhenAsync(UsingTheItemWithMode_, (byte)0)
                .Then(CannotTradeAskWearingQnaiShouldHaveBeenSent)
                .And(NoCanNotWearPacketSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SuccessfulEquipBindsTheItemToTheCharacter()
        {
            await new Spec("Successful equip sets BoundCharacterId to the character's id (needed for resale / rebind gates)")
                .Given(ItemInInventoryOfType_, ItemType.Armor)
                .And(CharacterIsLevel_, (byte)50)
                .WhenAsync(UsingTheItemWithMode_, (byte)1)
                .Then(ItemShouldBeBoundToCharacter)
                .And(NoCanNotWearPacketSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SuccessfulEquipAnnouncesTheWornItem()
        {
            await new Spec("Successful equip publishes which item is now worn")
                .Given(ItemInInventoryOfType_, ItemType.Armor)
                .And(CharacterIsLevel_, (byte)50)
                .WhenAsync(UsingTheItemWithMode_, (byte)1)
                .Then(TheWornItemShouldBePublished)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RefusedEquipAnnouncesNothing()
        {
            await new Spec("An item the character cannot wear publishes nothing")
                .Given(ItemInInventoryOfType_, ItemType.Armor)
                .And(ItemRequiresLevel_, (byte)99)
                .And(CharacterIsLevel_, (byte)1)
                .WhenAsync(UsingTheItemWithMode_, (byte)1)
                .Then(NoWornItemShouldBePublished)
                .ExecuteAsync();
        }

        private void TheWornItemShouldBePublished() =>
            _messageBus.Verify(b => b.PublishAsync(
                It.Is<NosCore.GameObject.Messaging.Events.ItemWornEvent>(e => e.VNum == _item.ItemInstance.ItemVNum),
                It.IsAny<Wolverine.DeliveryOptions?>()), Times.Once);

        private void NoWornItemShouldBePublished() =>
            _messageBus.Verify(b => b.PublishAsync(
                It.IsAny<NosCore.GameObject.Messaging.Events.ItemWornEvent>(),
                It.IsAny<Wolverine.DeliveryOptions?>()), Times.Never);

        [TestMethod]
        public async Task SuccessfulEquipWithValidTimeStampsItemDeleteTime()
        {
            await new Spec("Item with ItemValidTime > 0 gets an ItemDeleteTime stamped on successful equip (countdown starts on bind)")
                .Given(ItemInInventoryOfType_, ItemType.Armor)
                .And(ItemHasValidTime_, 3600)
                .And(CharacterIsLevel_, (byte)50)
                .WhenAsync(UsingTheItemWithMode_, (byte)1)
                .Then(ItemDeleteTimeShouldBeSet)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LowJobLevelIsRejectedWithTheJobLevelMessage()
        {
            await new Spec("An item whose LevelJobMinimum exceeds character.JobLevel is rejected, and the message says so - the class message belongs to the combined gate above")
                .Given(ItemInInventoryOfType_, ItemType.Specialist)
                .And(ItemRequiresJobLevel_, (byte)55)
                .And(CharacterIsJobLevel_, (byte)1)
                .WhenAsync(UsingTheItem)
                .Then(LowJobLevelShouldHaveBeenSent)
                .And(NoDifferentClassPacketSent)
                .ExecuteAsync();
        }

        private void ItemHasValidTime_(int seconds)
        {
            _item.ItemInstance.Item.ItemValidTime = seconds;
        }

        private void ItemShouldBeBoundToCharacter() =>
            Assert.AreEqual(_session.Character.CharacterId, _item.ItemInstance.BoundCharacterId);

        private void ItemDeleteTimeShouldBeSet() =>
            Assert.IsNotNull(_item.ItemInstance.ItemDeleteTime);

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

        private void ItemRequiresJobLevel_(byte level)
        {
            _item.ItemInstance.Item.LevelJobMinimum = level;
        }

        private void CharacterIsJobLevel_(byte level)
        {
            _session.Character.JobLevel = level;
        }

        private void LowJobLevelShouldHaveBeenSent()
        {
            var msg = _session.LastPackets.OfType<MsgiPacket>()
                .FirstOrDefault(p => p.Message == Game18NConstString.CanNotBeWornLowJobLevel);
            Assert.IsNotNull(msg);
        }

        // The failure this guards against sends a real message on the right condition, so it
        // raises nothing: the player is simply told the wrong reason.
        private void NoDifferentClassPacketSent()
        {
            var msg = _session.LastPackets.OfType<MsgiPacket>()
                .FirstOrDefault(p => p.Message == Game18NConstString.CanNotBeWornDifferentClass);
            Assert.IsNull(msg);
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

        private void CannotTradeAskWearingQnaiShouldHaveBeenSent()
        {
            var qnai = _session.LastPackets.OfType<QnaiPacket>().SingleOrDefault();
            Assert.IsNotNull(qnai);
            Assert.AreEqual(Game18NConstString.CannotTradeAskWearing, qnai.Question);
            var yes = qnai.YesPacket as UseItemPacket;
            Assert.IsNotNull(yes);
            Assert.AreEqual(1, yes.Mode);
            Assert.IsFalse(_session.LastPackets.OfType<QnaPacket>().Any());
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
