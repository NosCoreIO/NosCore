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
    public class SpecialistMedalHandlerTests
    {
        private const short MedalVNum = 1143;
        private const int MaxAdditionalSp = 100_000;
        private const int MaxSp = 10_000;
        private const int MedalValue = 2_000;
        private ClientSession _session = null!;
        private SpecialistMedalHandler _handler = null!;
        private InventoryItemInstance _medal = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            TestHelpers.Instance.WorldConfiguration.Value.MaxAdditionalSpPoints = MaxAdditionalSp;
            TestHelpers.Instance.WorldConfiguration.Value.MaxSpPoints = MaxSp;
            _handler = new SpecialistMedalHandler(TestHelpers.Instance.WorldConfiguration);
        }

        [TestMethod]
        public async Task MedalAddsBasicPointsAndThreeTimesAsManyAdditionalPoints()
        {
            await new Spec("A specialist medal is consumed and adds its value to the basic points and three times its value to the additional points")
                .Given(MedalInInventoryWith_Effect_, ItemEffectType.SpecialistMedal)
                .And(CharacterHasSpPoint_AndSpAdditionPoint_, 1_000, 10_000)
                .WhenAsync(UsingTheMedal)
                .Then(MedalStackCountShouldBe_, (short)0)
                .And(SpPointShouldBe_, 3_000)
                .And(SpAdditionPointShouldBe_, 16_000)
                .And(BasicPointsIncreasedMessageShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MedalIsCappedAtBothMaximums()
        {
            await new Spec("A specialist medal never raises either pool past its maximum")
                .Given(MedalInInventoryWith_Effect_, ItemEffectType.SpecialistMedal)
                .And(CharacterHasSpPoint_AndSpAdditionPoint_, MaxSp - 10, MaxAdditionalSp - 10)
                .WhenAsync(UsingTheMedal)
                .Then(MedalStackCountShouldBe_, (short)0)
                .And(SpPointShouldBe_, MaxSp)
                .And(SpAdditionPointShouldBe_, MaxAdditionalSp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MedalWithBothPoolsFullIsRejectedAndKept()
        {
            await new Spec("With both pools full the medal emits CannotBeUsedExceedsCapacity and is not consumed")
                .Given(MedalInInventoryWith_Effect_, ItemEffectType.SpecialistMedal)
                .And(CharacterHasSpPoint_AndSpAdditionPoint_, MaxSp, MaxAdditionalSp)
                .WhenAsync(UsingTheMedal)
                .Then(MedalStackCountShouldBe_, (short)1)
                .And(CannotBeUsedExceedsCapacityMessageShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OtherItemIsIgnored()
        {
            await new Spec("An item that is not a specialist medal is ignored")
                .Given(MedalInInventoryWith_Effect_, ItemEffectType.CraftedSpRecharger)
                .And(CharacterHasSpPoint_AndSpAdditionPoint_, 1_000, 10_000)
                .WhenAsync(UsingTheMedal)
                .Then(MedalStackCountShouldBe_, (short)1)
                .And(SpPointShouldBe_, 1_000)
                .And(SpAdditionPointShouldBe_, 10_000)
                .ExecuteAsync();
        }

        private void MedalInInventoryWith_Effect_(ItemEffectType effect)
        {
            var item = new Item
            {
                VNum = MedalVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Special,
                Effect = effect,
                EffectValue = MedalValue,
            };
            var inst = new ItemInstanceForTest(MedalVNum) { Amount = 1, Item = item };
            _medal = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _medal.Slot = 0;
            _medal.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_medal.ItemInstanceId] = _medal;
        }

        private void CharacterHasSpPoint_AndSpAdditionPoint_(int basic, int additional)
        {
            _session.Character.SpPoint = basic;
            _session.Character.SpAdditionPoint = additional;
        }

        private async Task UsingTheMedal()
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
            await _handler.Handle(new ItemUsedEvent(_session, _medal, packet));
        }

        private void MedalStackCountShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(MedalVNum));

        private void SpAdditionPointShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.SpAdditionPoint);

        private void SpPointShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.SpPoint);

        private void BasicPointsIncreasedMessageShouldBeSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>()
                .Any(m => m.Message == Game18NConstString.BasicSpecialistPointsIncreased));

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
