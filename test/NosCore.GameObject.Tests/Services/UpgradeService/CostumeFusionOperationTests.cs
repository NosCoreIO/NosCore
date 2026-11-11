//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.UpgradeService
{
    [TestClass]
    public class CostumeFusionOperationTests : SpecBase
    {
        private const short EffectsVNum = 189;
        private const short DesignVNum = 4795;
        private const short EffectsSlot = 0;
        private const short DesignSlot = 1;
        private const short FusionSlot = 0;

        private CostumeFusionOperation _operation;
        private WearableInstance _effects;
        private WearableInstance _design;
        private IReadOnlyList<IPacket> _result = Array.Empty<IPacket>();

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            _operation = new CostumeFusionOperation(new Mock<IRandomNumberSource>().Object,
                World.GameLanguageLocalizer);
            CharacterHasGold(1_500_000);
        }

        [TestMethod]
        public async Task TheFirstCostumeTakesTheLookOfTheSecond()
        {
            await new Spec("The first costume takes the second's look, the second is destroyed, and the gold and one fusion item are spent")
                .Given(TwoBoundPermanentSuits)
                .And(FusionItems_, CostumeFusionOperation.CostumeFusionVNum, (short)2)
                .WhenAsync(Fusing)
                .Then(TheEffectsCostumeShouldLookLike_, DesignVNum)
                .And(TheDesignCostumeShouldBeGone)
                .And(GoldShouldBe, 500_000L)
                .And(FusionItemsLeftShouldBe_, CostumeFusionVNum(), 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheBoundFusionItemIsSpentWhenItIsTheOnlyOne()
        {
            await new Spec("The bound Costume Fusion item pays for the fusion when it is the only one")
                .Given(TwoBoundPermanentSuits)
                .And(FusionItems_, CostumeFusionOperation.BoundCostumeFusionVNum, (short)1)
                .WhenAsync(Fusing)
                .Then(TheEffectsCostumeShouldLookLike_, DesignVNum)
                .And(FusionItemsLeftShouldBe_, CostumeFusionOperation.BoundCostumeFusionVNum, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutTheGoldNothingIsFused()
        {
            await new Spec("Without a million gold the fusion is refused and both costumes stay as they are")
                .Given(TwoBoundPermanentSuits)
                .And(FusionItems_, CostumeFusionOperation.CostumeFusionVNum, (short)1)
                .And(CharacterHasGold, 999_999L)
                .WhenAsync(Fusing)
                .Then(TheRefusalShouldBe_, Game18NConstString.NotEnoughGold)
                .And(TheEffectsCostumeShouldKeepItsLook)
                .And(TheDesignCostumeShouldStillBeThere)
                .And(FusionItemsLeftShouldBe_, CostumeFusionVNum(), 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutTheFusionItemNothingIsFused()
        {
            await new Spec("Without a Costume Fusion item the fusion is refused and no gold is spent")
                .Given(TwoBoundPermanentSuits)
                .WhenAsync(Fusing)
                .Then(TheRefusalShouldBe_, Game18NConstString.NotEnoughIngredients)
                .And(TheEffectsCostumeShouldKeepItsLook)
                .And(GoldShouldBe, 1_500_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATimedCostumeCannotBeFused()
        {
            await new Spec("A costume that expires cannot be fused")
                .Given(TwoBoundPermanentSuits)
                .And(TheDesignCostumeExpires)
                .And(FusionItems_, CostumeFusionOperation.CostumeFusionVNum, (short)1)
                .WhenAsync(Fusing)
                .Then(TheMessageShouldBe_, Game18NConstString.BothMaterialsMustBePermanent)
                .And(TheDesignCostumeShouldStillBeThere)
                .And(GoldShouldBe, 1_500_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnboundCostumeCannotBeFused()
        {
            await new Spec("A costume that is not bound to a character cannot be fused")
                .Given(TwoBoundPermanentSuits)
                .And(TheDesignCostumeIsNotBound)
                .And(FusionItems_, CostumeFusionOperation.CostumeFusionVNum, (short)1)
                .WhenAsync(Fusing)
                .Then(TheMessageShouldBe_, Game18NConstString.BothMaterialsMustBeBound)
                .And(TheDesignCostumeShouldStillBeThere)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFusedCostumeCannotBeFusedAgain()
        {
            await new Spec("A costume whose look was already changed cannot be fused again")
                .Given(TwoBoundPermanentSuits)
                .And(TheEffectsCostumeWasAlreadyFused)
                .And(FusionItems_, CostumeFusionOperation.CostumeFusionVNum, (short)1)
                .WhenAsync(Fusing)
                .Then(TheMessageShouldBe_, Game18NConstString.CannotUseItemWithChangedAppearance)
                .And(TheDesignCostumeShouldStillBeThere)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHatCannotLendItsLookToASuit()
        {
            await new Spec("A suit cannot take the look of a hat, and nothing is spent")
                .Given(ASuitAndAHat)
                .And(FusionItems_, CostumeFusionOperation.CostumeFusionVNum, (short)1)
                .WhenAsync(Fusing)
                .Then(NothingShouldBeSent)
                .And(TheEffectsCostumeShouldKeepItsLook)
                .And(GoldShouldBe, 1_500_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFusedCostumeIsWornWithItsNewLook()
        {
            await new Spec("Worn, a fused costume is shown to everyone with the look it took")
                .Given(TwoBoundPermanentSuits)
                .And(FusionItems_, CostumeFusionOperation.CostumeFusionVNum, (short)1)
                .WhenAsync(FusingThenWearingTheCostume)
                .Then(TheWornSuitShouldShow_, DesignVNum)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingTheFusionItemOpensTheWindow()
        {
            await new Spec("Using a Costume Fusion item opens the fusion window and keeps the item")
                .Given(FusionItems_, CostumeFusionOperation.CostumeFusionVNum, (short)1)
                .WhenAsync(UsingTheFusionItem)
                .Then(TheWindowShouldBe_, WindowType.FusionItem)
                .And(TheItemWindowShouldBeOpen)
                .And(FusionItemsLeftShouldBe_, CostumeFusionOperation.CostumeFusionVNum, 1)
                .ExecuteAsync();
        }

        private static short CostumeFusionVNum() => CostumeFusionOperation.CostumeFusionVNum;

        private void TwoBoundPermanentSuits()
        {
            _effects = PlaceCostume(EffectsVNum, EquipmentType.CostumeSuit, EffectsSlot);
            _design = PlaceCostume(DesignVNum, EquipmentType.CostumeSuit, DesignSlot);
        }

        private void ASuitAndAHat()
        {
            _effects = PlaceCostume(EffectsVNum, EquipmentType.CostumeSuit, EffectsSlot);
            _design = PlaceCostume(DesignVNum, EquipmentType.CostumeHat, DesignSlot);
        }

        private WearableInstance PlaceCostume(short vnum, EquipmentType slot, short bagSlot)
        {
            var costume = new WearableInstance(new Item
            {
                VNum = vnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion, EquipmentSlot = slot,
            }, NullLogger<WearableInstance>.Instance, World.LogLanguageLocalizer)
            {
                Amount = 1,
                BoundCharacterId = Session.Character.CharacterId,
            };
            var inst = InventoryItemInstance.Create(costume, Session.Character.CharacterId);
            inst.Slot = bagSlot;
            inst.Type = NoscorePocketType.Equipment;
            Session.Character.InventoryService[inst.ItemInstanceId] = inst;
            return costume;
        }

        private void FusionItems_(short vnum, short amount)
        {
            var inst = InventoryItemInstance.Create(
                new ItemInstance(new Item { VNum = vnum, Type = NoscorePocketType.Main }) { Amount = amount },
                Session.Character.CharacterId);
            inst.Slot = FusionSlot;
            inst.Type = NoscorePocketType.Main;
            Session.Character.InventoryService[inst.ItemInstanceId] = inst;
        }

        private void TheDesignCostumeExpires() => _design.Item.ItemValidTime = 30 * 24 * 3600;

        private void TheDesignCostumeIsNotBound() => _design.BoundCharacterId = null;

        private void TheEffectsCostumeWasAlreadyFused() => _effects.AppearanceVNum = 186;

        private void TheEffectsCostumeIsWorn()
        {
            var worn = Session.Character.InventoryService.Values.Single(i => i.ItemInstance == _effects);
            worn.Type = NoscorePocketType.Wear;
            worn.Slot = (short)EquipmentType.CostumeSuit;
        }

        private async Task Fusing() =>
            _result = await _operation.ExecuteAsync(Session, new UpgradePacket
            {
                UpgradeType = UpgradePacketType.FusionItem,
                InventoryType = PocketType.Equipment,
                Slot = (byte)EffectsSlot,
                InventoryType2 = PocketType.Equipment,
                Slot2 = (byte)DesignSlot,
            });

        private async Task FusingThenWearingTheCostume()
        {
            await Fusing();
            TheEffectsCostumeIsWorn();
        }

        private Task UsingTheFusionItem()
        {
            var item = Session.Character.InventoryService.LoadBySlotAndType(FusionSlot, NoscorePocketType.Main).NotNull();
            return new UpgradeWindowItemHandler(new IUpgradeOperation[] { _operation }, World.NpcDialogService)
                .Handle(new ItemUsedEvent(Session, item, new UseItemPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = Session.Character.CharacterId,
                    Type = PocketType.Main,
                    Slot = (byte)FusionSlot,
                }));
        }

        private void TheEffectsCostumeShouldLookLike_(short vnum) => Assert.AreEqual(vnum, _effects.AppearanceVNum);

        private void TheEffectsCostumeShouldKeepItsLook() => Assert.IsNull(_effects.AppearanceVNum);

        private void TheDesignCostumeShouldBeGone() =>
            Assert.IsFalse(Session.Character.InventoryService.Values.Any(i => i.ItemInstance == _design));

        private void TheDesignCostumeShouldStillBeThere() =>
            Assert.IsTrue(Session.Character.InventoryService.Values.Any(i => i.ItemInstance == _design));

        private void FusionItemsLeftShouldBe_(short vnum, int amount) =>
            Assert.AreEqual(amount, Session.Character.InventoryService.CountItem(vnum));

        private void TheRefusalShouldBe_(Game18NConstString message) =>
            Assert.AreEqual(message, _result.OfType<InfoiPacket>().Single().Message);

        private void TheMessageShouldBe_(Game18NConstString message) =>
            Assert.AreEqual(message, _result.OfType<MsgiPacket>().Single().Message);

        private void NothingShouldBeSent() => Assert.AreEqual(0, _result.Count);

        private void TheWornSuitShouldShow_(short vnum) =>
            Assert.AreEqual(vnum, Session.Character.GetEquipmentSubPacket().CostumeSuit);

        private void TheWindowShouldBe_(WindowType window) =>
            Assert.AreEqual(window, GetLastPacket<WopenPacket>().NotNull().Type);

        private void TheItemWindowShouldBeOpen() =>
            Assert.IsTrue(World.NpcDialogService.IsOpen(Session.Character, NpcDialogWindow.UpgradeScroll));
    }
}
