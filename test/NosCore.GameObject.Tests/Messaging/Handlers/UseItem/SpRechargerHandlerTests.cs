//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
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
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class SpRechargerHandlerTests
    {
        private const short RechargerVNum = 5500;
        private const int MaxAdditionalSp = 100_000;
        private ClientSession _session = null!;
        private SpRechargerHandler _handler = null!;
        private InventoryItemInstance _recharger = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            TestHelpers.Instance.WorldConfiguration.Value.MaxAdditionalSpPoints = MaxAdditionalSp;
            _handler = new SpRechargerHandler(TestHelpers.Instance.WorldConfiguration);
        }

        [TestMethod]
        public async Task RechargerBelowCapIsConsumedAndAddsSpPoints()
        {
            await new Spec("A recharger consumed below the SP-addition cap debits one item and increases SpAdditionPoint by EffectValue")
                .Given(RechargerInInventoryWith_EffectAndValue_, ItemEffectType.CraftedSpRecharger, 30_000)
                .And(CharacterHasSpAdditionPoint_, 10_000)
                .WhenAsync(UsingTheRecharger)
                .Then(RechargerStackCountShouldBe_, (short)0)
                .And(SpAdditionPointShouldBe_, 40_000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RechargerAtCapIsRejectedWithMessageAndNotConsumed()
        {
            await new Spec("At SP-addition cap the recharger emits CannotBeUsedExceedsCapacity and is not consumed")
                .Given(RechargerInInventoryWith_EffectAndValue_, ItemEffectType.CraftedSpRecharger, 30_000)
                .And(CharacterHasSpAdditionPoint_, MaxAdditionalSp)
                .WhenAsync(UsingTheRecharger)
                .Then(RechargerStackCountShouldBe_, (short)1)
                .And(SpAdditionPointShouldBe_, MaxAdditionalSp)
                .And(CannotBeUsedExceedsCapacityMessageShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NonRechargerItemIsIgnored()
        {
            await new Spec("An item that is not a SpRecharger is ignored by the handler (no item consumption, no SP change)")
                .Given(RechargerInInventoryWith_EffectAndValue_, ItemEffectType.Teleport, 999)
                .And(CharacterHasSpAdditionPoint_, 10_000)
                .WhenAsync(UsingTheRecharger)
                .Then(RechargerStackCountShouldBe_, (short)1)
                .And(SpAdditionPointShouldBe_, 10_000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RechargerAddsCappedAtMaxEvenIfEffectValueWouldOverflow()
        {
            await new Spec("AddAdditionalSpPoints clamps the result so the cap is never exceeded")
                .Given(RechargerInInventoryWith_EffectAndValue_, ItemEffectType.CraftedSpRecharger, 50_000)
                .And(CharacterHasSpAdditionPoint_, MaxAdditionalSp - 10)
                .WhenAsync(UsingTheRecharger)
                .Then(RechargerStackCountShouldBe_, (short)0)
                .And(SpAdditionPointShouldBe_, MaxAdditionalSp)
                .ExecuteAsync();
        }

        private void RechargerInInventoryWith_EffectAndValue_(ItemEffectType effect, int effectValue)
        {
            var item = new Item
            {
                VNum = RechargerVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Special,
                Effect = effect,
                EffectValue = effectValue,
            };
            var inst = new ItemInstanceForTest(RechargerVNum) { Amount = 1, Item = item };
            _recharger = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _recharger.Slot = 0;
            _recharger.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_recharger.ItemInstanceId] = _recharger;
        }

        private void CharacterHasSpAdditionPoint_(int points)
        {
            _session.Character.SpAdditionPoint = points;
        }

        private async Task UsingTheRecharger()
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
            await _handler.Handle(new ItemUsedEvent(_session, _recharger, packet));
        }

        private void RechargerStackCountShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(RechargerVNum));

        private void SpAdditionPointShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.SpAdditionPoint);

        private void CannotBeUsedExceedsCapacityMessageShouldBeSent()
        {
            var msg = _session.LastPackets.OfType<MsgiPacket>()
                .FirstOrDefault(m => m.Message == Game18NConstString.CannotBeUsedExceedsCapacity);
            Assert.IsNotNull(msg);
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
