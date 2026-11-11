//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class SpecialistWingsHandlerTests
    {
        private TestWorld World;
        private const short WingsVNum = 1686;
        private const int DevilWings = 2;
        private ClientSession _session;
        private SpecialistWingsHandler _handler;
        private InventoryItemInstance _wings;
        private InventoryItemInstance _sp;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.GenerateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(1);
            _handler = new SpecialistWingsHandler();
            WingsInInventory();
        }

        [TestMethod]
        public async Task ConfirmedWingsChangeTheSpecialistDesign()
        {
            await new Spec("Confirming the question gives the worn specialist the item's wings and uses the item up")
                .Given(ASpecialistUpgradedTo_IsWorn, (byte)1)
                .And(CharacterIsTransformed)
                .WhenAsync(UsingTheWingsWithMode_, (byte)1)
                .Then(SpecialistDesignShouldBe_, (short)DevilWings)
                .And(CharacterMorphDesignShouldBe_, (short)DevilWings)
                .And(WingsCountShouldBe_, 0)
                .And(Message_ShouldBeSent, Game18NConstString.WingsChanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FirstUseAsksBeforeChangingTheWings()
        {
            await new Spec("The first use only asks whether to change the wings")
                .Given(ASpecialistUpgradedTo_IsWorn, (byte)1)
                .And(CharacterIsTransformed)
                .WhenAsync(UsingTheWingsWithMode_, (byte)0)
                .Then(TheChangeWingsQuestionShouldBeAsked)
                .And(SpecialistDesignShouldBe_, (short)0)
                .And(WingsCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WingsNeedATransformedCharacter()
        {
            await new Spec("Without a specialist transformation the item is kept and the player is told to transform")
                .Given(ASpecialistUpgradedTo_IsWorn, (byte)1)
                .WhenAsync(UsingTheWingsWithMode_, (byte)1)
                .Then(Message_ShouldBeSent, Game18NConstString.TransformToChangeWings)
                .And(SpecialistDesignShouldBe_, (short)0)
                .And(WingsCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WingsNeedASpecialistAtPlusOneOrMore()
        {
            await new Spec("A +0 specialist cannot take wings")
                .Given(ASpecialistUpgradedTo_IsWorn, (byte)0)
                .And(CharacterIsTransformed)
                .WhenAsync(UsingTheWingsWithMode_, (byte)1)
                .Then(Message_ShouldBeSent, Game18NConstString.WingsOnlyFromPlusOne)
                .And(SpecialistDesignShouldBe_, (short)0)
                .And(WingsCountShouldBe_, 1)
                .ExecuteAsync();
        }

        private void WingsInInventory()
        {
            var item = new Item
            {
                VNum = WingsVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Special,
                Effect = ItemEffectType.ChangeSpecialistWings,
                EffectValue = DevilWings,
            };
            var inst = new ItemInstanceForTest(WingsVNum) { Amount = 1, Item = item };
            _wings = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _wings.Slot = 0;
            _wings.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_wings.ItemInstanceId] = _wings;
        }

        private void ASpecialistUpgradedTo_IsWorn(byte upgrade)
        {
            var spInstance = World.GenerateItemProvider().Create(912, 1);
            spInstance.Upgrade = upgrade;
            _sp = InventoryItemInstance.Create(spInstance, _session.Character.CharacterId);
            _sp.Type = NoscorePocketType.Wear;
            _sp.Slot = (byte)EquipmentType.Sp;
            _session.Character.InventoryService[_sp.ItemInstanceId] = _sp;
        }

        private void CharacterIsTransformed() => _session.Character.UseSp = true;

        private async Task UsingTheWingsWithMode_(byte mode)
        {
            await _handler.Handle(new ItemUsedEvent(_session, _wings, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = mode,
                Parameter = 0,
            }));
        }

        private void SpecialistDesignShouldBe_(short design) => Assert.AreEqual(design, _sp.ItemInstance.Design);

        private void CharacterMorphDesignShouldBe_(short design) => Assert.AreEqual(design, _session.Character.MorphDesign);

        private void WingsCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(WingsVNum));

        private void Message_ShouldBeSent(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(m => m.Message == message));

        private void TheChangeWingsQuestionShouldBeAsked()
        {
            var qna = _session.LastPackets.OfType<QnaiPacket>().SingleOrDefault();
            Assert.IsNotNull(qna);
            Assert.AreEqual(Game18NConstString.AskChangeWings, qna.Question);
            Assert.AreEqual((byte)1, ((UseItemPacket)qna.YesPacket.NotNull()).Mode);
        }

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
