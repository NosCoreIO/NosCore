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
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class MinilandBellHandlerTests
    {
        private const short BellVNum = 1244;
        private ClientSession _session = null!;
        private Mock<IMinilandService> _minilandService = null!;
        private Mock<IMapChangeService> _mapChangeService = null!;
        private MinilandBellHandler _handler = null!;
        private InventoryItemInstance _bell = null!;
        private readonly Guid _minilandInstanceId = Guid.NewGuid();

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _minilandService = new Mock<IMinilandService>();
            _mapChangeService = new Mock<IMapChangeService>();

            _minilandService.Setup(x => x.GetMiniland(It.IsAny<long>()))
                .Returns(new Miniland { MapInstanceId = _minilandInstanceId, MinilandMessage = "Test" });

            _handler = new MinilandBellHandler(_minilandService.Object, _mapChangeService.Object);
        }

        [TestMethod]
        public async Task NonTeleportItemIsIgnored()
        {
            await new Spec("Bell handler ignores items whose Effect is not Teleport(Value=2)")
                .Given(BellInInventoryWithEffect_AndValue_, ItemEffectType.NoEffect, (short)0)
                .And(CharacterIsOnBaseMap)
                .WhenAsync(UsingBellWithMode_, (byte)1)
                .Then(NoPacketShouldBeSent)
                .And(ChangeMapInstanceAsyncWasNotCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingBellOutsideBaseMapIsRejectedWithCanNotBeUsedHere()
        {
            await new Spec("Bell handler rejects use from a non-base instance with CanNotBeUsedHere")
                .Given(BellInInventoryWithEffect_AndValue_, ItemEffectType.Teleport, (short)2)
                .And(CharacterIsInsideNonBaseInstance)
                .WhenAsync(UsingBellWithMode_, (byte)1)
                .Then(SayMessageShouldBe_, Game18NConstString.CanNotBeUsedHere)
                .And(ChangeMapInstanceAsyncWasNotCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingBellWhileVehicledIsRejectedWithOnlyPotionInVehicle()
        {
            await new Spec("Bell handler rejects use while vehicled with OnlyPotionInVehicle")
                .Given(BellInInventoryWithEffect_AndValue_, ItemEffectType.Teleport, (short)2)
                .And(CharacterIsOnBaseMap)
                .And(CharacterIsVehicled)
                .WhenAsync(UsingBellWithMode_, (byte)1)
                .Then(SayMessageShouldBe_, Game18NConstString.OnlyPotionInVehicle)
                .And(ChangeMapInstanceAsyncWasNotCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingBellWithModeZeroEmitsDelayPacketAndDoesNotTeleport()
        {
            await new Spec("Bell handler with Mode=0 emits a DelayPacket (first stage) and does not teleport")
                .Given(BellInInventoryWithEffect_AndValue_, ItemEffectType.Teleport, (short)2)
                .And(CharacterIsOnBaseMap)
                .WhenAsync(UsingBellWithMode_, (byte)0)
                .Then(DelayPacketShouldHaveBeenSent)
                .And(ChangeMapInstanceAsyncWasNotCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingBellWithNonZeroModeConsumesItemAndTeleportsToMinilandAt5And8()
        {
            await new Spec("Bell handler with Mode=1 consumes one bell and teleports the player to miniland at 5/8")
                .Given(BellInInventoryWithEffect_AndValue_, ItemEffectType.Teleport, (short)2)
                .And(CharacterIsOnBaseMap)
                .WhenAsync(UsingBellWithMode_, (byte)1)
                .Then(BellStackCountShouldBe_, (short)0)
                .And(ChangeMapInstanceAsyncWasCalledForMinilandWith_, 5, 8)
                .ExecuteAsync();
        }

        private void BellInInventoryWithEffect_AndValue_(ItemEffectType effect, short value)
        {
            var item = new Item
            {
                VNum = BellVNum,
                Type = NoscorePocketType.Main,
                Effect = effect,
                EffectValue = value,
            };
            var instance = new ItemInstanceForTest(BellVNum) { Amount = 1, Item = item };
            _bell = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _bell.Slot = 0;
            _bell.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_bell.ItemInstanceId] = _bell;
        }

        private void CharacterIsOnBaseMap()
        {
            _session.Character.MapInstance.MapInstanceType = MapInstanceType.BaseMapInstance;
        }

        private void CharacterIsInsideNonBaseInstance()
        {
            _session.Character.MapInstance.MapInstanceType = MapInstanceType.NormalInstance;
        }

        private void CharacterIsVehicled()
        {
            _session.Character.IsVehicled = true;
        }

        private async Task UsingBellWithMode_(byte mode)
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
            await _handler.Handle(new ItemUsedEvent(_session, _bell, packet));
        }

        private void NoPacketShouldBeSent() =>
            Assert.AreEqual(0, _session.LastPackets.Count);

        private void SayMessageShouldBe_(Game18NConstString expected)
        {
            var say = _session.LastPackets.OfType<SayiPacket>().FirstOrDefault();
            Assert.IsNotNull(say);
            Assert.AreEqual(expected, say.Message);
        }

        private void DelayPacketShouldHaveBeenSent()
        {
            Assert.IsTrue(_session.LastPackets.OfType<DelayPacket>().Any());
        }

        private void BellStackCountShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(BellVNum));

        private void ChangeMapInstanceAsyncWasCalledForMinilandWith_(int x, int y)
        {
            _mapChangeService.Verify(m => m.ChangeMapInstanceAsync(
                _session,
                _minilandInstanceId,
                It.Is<int?>(v => v == x),
                It.Is<int?>(v => v == y)), Times.Once);
        }

        private void ChangeMapInstanceAsyncWasNotCalled()
        {
            _mapChangeService.Verify(m => m.ChangeMapInstanceAsync(
                It.IsAny<ClientSession>(), It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
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
