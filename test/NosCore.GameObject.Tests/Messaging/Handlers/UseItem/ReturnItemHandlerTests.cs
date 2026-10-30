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
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class ReturnItemHandlerTests
    {
        private const short ItemVNum = 2070;
        private ClientSession _session = null!;
        private Mock<IRespawnService> _respawnService = null!;
        private Mock<IMapChangeService> _mapChangeService = null!;
        private ReturnItemHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MapInstance.MapInstanceType = MapInstanceType.BaseMapInstance;
            _respawnService = new Mock<IRespawnService>();
            _mapChangeService = new Mock<IMapChangeService>();
            _respawnService.Setup(x => x.ResolveRespawnMapTypeId(It.IsAny<short>())).Returns(0);
            _respawnService.Setup(x => x.GetRespawnLocation(It.IsAny<ICharacterEntity>(), It.IsAny<long?>()))
                .Returns(((short)1, (short)80, (short)116));
            _handler = new ReturnItemHandler(_respawnService.Object, _mapChangeService.Object);
        }

        [TestMethod]
        public async Task OtherTeleportItemIsIgnored()
        {
            await new Spec("Return handler ignores the Bell of Sweet Home")
                .Given(ItemInInventory_, TeleportEffectValue.Miniland)
                .WhenAsync(UsingItemWithMode_, (byte)2)
                .Then(NoPacketShouldBeSent)
                .And(NoMapChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WingOutsideBaseMapIsRejected()
        {
            await new Spec("Wing of Return is rejected inside an instance")
                .Given(ItemInInventory_, TeleportEffectValue.WingOfReturn)
                .And(CharacterIsInsideNonBaseInstance)
                .WhenAsync(UsingItemWithMode_, (byte)0)
                .Then(SayMessageShouldBe_, Game18NConstString.CanNotBeUsedHere)
                .And(NoMapChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WingAsksWhetherToSaveThePosition()
        {
            await new Spec("Wing of Return asks whether to save the position before leaving")
                .Given(ItemInInventory_, TeleportEffectValue.WingOfReturn)
                .WhenAsync(UsingItemWithMode_, (byte)0)
                .Then(SaveQuestionShouldBeAsked)
                .And(NoMapChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmStartsTheDelayToSaveAndLeave()
        {
            await new Spec("Confirming the save question starts the delay bar towards saving and leaving")
                .Given(ItemInInventory_, TeleportEffectValue.WingOfReturn)
                .WhenAsync(UsingItemWithMode_, (byte)1)
                .Then(DelayPacketShouldReplayMode_, (byte)3)
                .And(NoMapChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CancelStillStartsTheDelayToLeave()
        {
            await new Spec("Cancelling the save question still starts the delay bar, towards leaving without saving")
                .Given(ItemInInventory_, TeleportEffectValue.WingOfReturn)
                .WhenAsync(UsingItemWithMode_, (byte)2)
                .Then(DelayPacketShouldReplayMode_, (byte)4)
                .And(NoMapChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WingSavesTheSpotAndSendsToRevivalPoint()
        {
            await new Spec("After a confirmed save the wing is consumed, saves the spot and warps to the place of revival")
                .Given(ItemInInventory_, TeleportEffectValue.WingOfReturn)
                .WhenAsync(UsingItemWithMode_, (byte)3)
                .Then(ItemShouldBeConsumed)
                .And(ReturnPointShouldBeSavedAtCurrentPosition)
                .And(MapChangeShouldBeTo_, (short)1, (short)80, (short)116)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CancelledWingSendsToRevivalPointWithoutSaving()
        {
            await new Spec("After a cancelled save the wing is still consumed and warps to the place of revival, keeping the old point")
                .Given(ItemInInventory_, TeleportEffectValue.WingOfReturn)
                .And(AReturnPointIsSaved)
                .WhenAsync(UsingItemWithMode_, (byte)4)
                .Then(ItemShouldBeConsumed)
                .And(ReturnPointShouldNotBeSaved)
                .And(MapChangeShouldBeTo_, (short)1, (short)80, (short)116)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AmuletWithoutReturnPointIsRejected()
        {
            await new Spec("Amulet of Return without a saved point says so and keeps the item")
                .Given(ItemInInventory_, TeleportEffectValue.AmuletOfReturn)
                .WhenAsync(UsingItemWithMode_, (byte)0)
                .Then(SayMessageShouldBe_, Game18NConstString.NoReturnPoint)
                .And(NoMapChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AmuletMarksTheReturnPoint()
        {
            await new Spec("Amulet of Return with a saved point marks it on the map before leaving")
                .Given(ItemInInventory_, TeleportEffectValue.AmuletOfReturn)
                .And(AReturnPointIsSaved)
                .WhenAsync(UsingItemWithMode_, (byte)0)
                .Then(ReturnPointShouldBeMarked)
                .And(NoMapChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmedAmuletStartsTheDelay()
        {
            await new Spec("Clicking the marked point starts the delay bar")
                .Given(ItemInInventory_, TeleportEffectValue.AmuletOfReturn)
                .And(AReturnPointIsSaved)
                .WhenAsync(UsingItemWithMode_, (byte)1)
                .Then(DelayPacketShouldReplayMode_, (byte)2)
                .And(NoMapChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AmuletSendsBackToTheReturnPoint()
        {
            await new Spec("After the delay the Amulet of Return is consumed and warps to the saved point")
                .Given(ItemInInventory_, TeleportEffectValue.AmuletOfReturn)
                .And(AReturnPointIsSaved)
                .WhenAsync(UsingItemWithMode_, (byte)2)
                .Then(ItemShouldBeConsumed)
                .And(MapChangeShouldBeTo_, (short)2550, (short)20, (short)33)
                .ExecuteAsync();
        }

        private void ItemInInventory_(TeleportEffectValue value)
        {
            var item = new Item
            {
                VNum = ItemVNum,
                Type = NoscorePocketType.Main,
                Effect = ItemEffectType.Teleport,
                EffectValue = (short)value,
            };
            var instance = new ItemInstanceForTest(ItemVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void CharacterIsInsideNonBaseInstance()
        {
            _session.Character.MapInstance.MapInstanceType = MapInstanceType.NormalInstance;
        }

        private void AReturnPointIsSaved()
        {
            _respawnService.Setup(x => x.GetReturnPoint(It.IsAny<ICharacterEntity>(), It.IsAny<short>()))
                .Returns(((short)2550, (short)20, (short)33));
        }

        private async Task UsingItemWithMode_(byte mode)
        {
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = mode,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _item, packet));
        }

        private void NoPacketShouldBeSent() =>
            Assert.AreEqual(0, _session.LastPackets.Count);

        private void SayMessageShouldBe_(Game18NConstString expected)
        {
            var say = _session.LastPackets.OfType<SayiPacket>().FirstOrDefault();
            Assert.IsNotNull(say);
            Assert.AreEqual(expected, say.Message);
        }

        private void DelayPacketShouldReplayMode_(byte mode)
        {
            var useItem = (UseItemPacket)_session.LastPackets.OfType<DelayPacket>().Single().Packet!;
            Assert.AreEqual(mode, useItem.Mode);
        }

        private void SaveQuestionShouldBeAsked()
        {
            var dialog = _session.LastPackets.OfType<DlgiPacket>().Single();
            Assert.AreEqual(Game18NConstString.AmuletMessageToSavePositionAndReturnToVillage, dialog.Question);
            Assert.AreEqual((byte)1, ((UseItemPacket)dialog.YesPacket!).Mode);
            Assert.AreEqual((byte)2, ((UseItemPacket)dialog.NoPacket!).Mode);
            Assert.IsFalse(_session.LastPackets.OfType<DelayPacket>().Any());
        }

        private void ReturnPointShouldBeMarked()
        {
            var rp = _session.LastPackets.OfType<RpPacket>().Single();
            Assert.AreEqual((2550, 20, 33), (rp.MapId, rp.X, rp.Y));
            Assert.AreEqual((byte)1, ((UseItemPacket)rp.Packet!).Mode);
            Assert.IsFalse(_session.LastPackets.OfType<DelayPacket>().Any());
        }

        private void ItemShouldBeConsumed() =>
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(ItemVNum));

        private void ReturnPointShouldBeSavedAtCurrentPosition()
        {
            var character = _session.Character;
            _respawnService.Verify(x => x.SetReturnPoint(character, character.MapInstance.Map.MapId,
                character.PositionX, character.PositionY), Times.Once);
        }

        private void ReturnPointShouldNotBeSaved()
        {
            _respawnService.Verify(x => x.SetReturnPoint(It.IsAny<ICharacterEntity>(), It.IsAny<short>(),
                It.IsAny<short>(), It.IsAny<short>()), Times.Never);
        }

        private void MapChangeShouldBeTo_(short mapId, short x, short y)
        {
            _mapChangeService.Verify(m => m.ChangeMapAsync(_session, mapId, x, y), Times.Once);
        }

        private void NoMapChange()
        {
            _mapChangeService.Verify(m => m.ChangeMapAsync(
                It.IsAny<ClientSession>(), It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()), Times.Never);
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
