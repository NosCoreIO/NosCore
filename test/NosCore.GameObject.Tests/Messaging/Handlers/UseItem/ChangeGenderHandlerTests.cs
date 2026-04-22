//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
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
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class ChangeGenderHandlerTests
    {
        private const short GenderScrollVNum = 5105;
        private ClientSession _session = null!;
        private ChangeGenderHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _handler = new ChangeGenderHandler();
        }

        [TestMethod]
        public async Task WrongEffectIsIgnored()
        {
            await new Spec("Item with a non-ChangeGender Effect is a no-op")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.Teleport)
                .And(CharacterGenderIs_, GenderType.Male)
                .WhenAsync(UsingTheItem)
                .Then(GenderShouldBe_, GenderType.Male)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task VehicledCharacterIsRejected()
        {
            await new Spec("Vehicled character is blocked from switching gender")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.ChangeGender)
                .And(CharacterGenderIs_, GenderType.Male)
                .And(CharacterIsVehicled)
                .WhenAsync(UsingTheItem)
                .Then(GenderShouldBe_, GenderType.Male)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CharacterWithWornItemsIsRejected()
        {
            await new Spec("Character with any equipped (Wear-pocket) item is blocked from switching gender")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.ChangeGender)
                .And(CharacterGenderIs_, GenderType.Male)
                .And(CharacterHasAnEquippedItem)
                .WhenAsync(UsingTheItem)
                .Then(GenderShouldBe_, GenderType.Male)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MaleCharacterFlipsToFemale()
        {
            await new Spec("Male character flips to Female and consumes the item")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.ChangeGender)
                .And(CharacterGenderIs_, GenderType.Male)
                .WhenAsync(UsingTheItem)
                .Then(GenderShouldBe_, GenderType.Female)
                .And(ItemStackCountShouldBe_, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FemaleCharacterFlipsToMale()
        {
            await new Spec("Female character flips to Male and consumes the item")
                .Given(ItemInInventoryWithEffect_, ItemEffectType.ChangeGender)
                .And(CharacterGenderIs_, GenderType.Female)
                .WhenAsync(UsingTheItem)
                .Then(GenderShouldBe_, GenderType.Male)
                .And(ItemStackCountShouldBe_, (short)0)
                .ExecuteAsync();
        }

        private void ItemInInventoryWithEffect_(ItemEffectType effect)
        {
            var item = new Item
            {
                VNum = GenderScrollVNum,
                Type = NoscorePocketType.Main,
                Effect = effect,
            };
            var inst = new ItemInstanceForTest(GenderScrollVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void CharacterGenderIs_(GenderType gender) => _session.Character.Gender = gender;
        private void CharacterIsVehicled() => _session.Character.IsVehicled = true;

        private void CharacterHasAnEquippedItem()
        {
            var armorItem = new Item { VNum = 1, Type = NoscorePocketType.Equipment };
            var armor = new ItemInstanceForTest(1) { Amount = 1, Item = armorItem };
            var worn = InventoryItemInstance.Create(armor, _session.Character.CharacterId);
            worn.Slot = 0;
            worn.Type = NoscorePocketType.Wear;
            _session.Character.InventoryService[worn.ItemInstanceId] = worn;
        }

        private async Task UsingTheItem()
        {
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 1,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _item, packet));
        }

        private void GenderShouldBe_(GenderType expected) =>
            Assert.AreEqual(expected, _session.Character.Gender);

        private void ItemStackCountShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(GenderScrollVNum));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
