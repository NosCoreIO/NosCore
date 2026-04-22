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
    public class HairDieHandlerTests
    {
        private const short DieVNum = 2060;
        private ClientSession _session = null!;
        private HairDieHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _handler = new HairDieHandler();
        }

        [TestMethod]
        public async Task WrongEffectIsIgnored()
        {
            await new Spec("Item with an unrelated Effect is a no-op (no color change, item preserved)")
                .Given(ItemInInventoryWithEffect_Value_, ItemEffectType.Teleport, 0)
                .And(CharacterHairColorIs_, HairColorType.Black)
                .WhenAsync(UsingTheItem)
                .Then(HairColorShouldBe_, HairColorType.Black)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task VehicledCharacterCannotApplyDye()
        {
            await new Spec("Vehicled character returns early without applying the dye")
                .Given(ItemInInventoryWithEffect_Value_, ItemEffectType.ApplyHairDie, 5)
                .And(CharacterHairColorIs_, HairColorType.Black)
                .And(CharacterIsVehicled)
                .WhenAsync(UsingTheItem)
                .Then(HairColorShouldBe_, HairColorType.Black)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ApplyHairDieSetsHairColorFromEffectValue()
        {
            await new Spec("Valid ApplyHairDie sets HairColor to EffectValue and consumes the item")
                .Given(ItemInInventoryWithEffect_Value_, ItemEffectType.ApplyHairDie, 3)
                .And(CharacterHairColorIs_, HairColorType.Black)
                .WhenAsync(UsingTheItem)
                .Then(HairColorShouldBe_, (HairColorType)3)
                .And(ItemStackCountShouldBe_, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ApplyHairStyleSetsHairStyleFromEffectValue()
        {
            await new Spec("Valid ApplyHairStyle sets HairStyle to EffectValue and consumes the item")
                .Given(ItemInInventoryWithEffect_Value_, ItemEffectType.ApplyHairStyle, 2)
                .And(CharacterHairStyleIs_, HairStyleType.HairStyleA)
                .WhenAsync(UsingTheItem)
                .Then(HairStyleShouldBe_, (HairStyleType)2)
                .And(ItemStackCountShouldBe_, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UndefinedColorFallsBackToDarkPurple()
        {
            await new Spec("EffectValue outside defined HairColorType falls back to DarkPurple")
                .Given(ItemInInventoryWithEffect_Value_, ItemEffectType.ApplyHairDie, 200)
                .WhenAsync(UsingTheItem)
                .Then(HairColorShouldBe_, HairColorType.DarkPurple)
                .ExecuteAsync();
        }

        private void ItemInInventoryWithEffect_Value_(ItemEffectType effect, int value)
        {
            var item = new Item
            {
                VNum = DieVNum,
                Type = NoscorePocketType.Main,
                Effect = effect,
                EffectValue = value,
            };
            var inst = new ItemInstanceForTest(DieVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void CharacterHairColorIs_(HairColorType color) => _session.Character.HairColor = color;
        private void CharacterHairStyleIs_(HairStyleType style) => _session.Character.HairStyle = style;
        private void CharacterIsVehicled() => _session.Character.IsVehicled = true;

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

        private void HairColorShouldBe_(HairColorType expected) =>
            Assert.AreEqual(expected, _session.Character.HairColor);

        private void HairStyleShouldBe_(HairStyleType expected) =>
            Assert.AreEqual(expected, _session.Character.HairStyle);

        private void ItemStackCountShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(DieVNum));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
