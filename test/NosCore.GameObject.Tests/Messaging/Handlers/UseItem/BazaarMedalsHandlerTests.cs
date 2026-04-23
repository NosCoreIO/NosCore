//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
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
    public class BazaarMedalsHandlerTests
    {
        private const short MedalVNum = 5108;
        private ClientSession _session = null!;
        private BazaarMedalsHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _handler = new BazaarMedalsHandler(TestHelpers.Instance.Clock);
        }

        [TestMethod]
        public async Task UnrelatedEffectIsIgnored()
        {
            await new Spec("A non-NosMerchant effect grants no bonus and is not consumed")
                .Given(ItemInInventoryWithEffectAndDays_, ItemEffectType.Teleport, 30)
                .WhenAsync(UsingTheItem)
                .Then(StaticBonusCountShouldBe_, 0)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SilverMerchantGrantsSilverBonusAndConsumesItem()
        {
            await new Spec("SilverNosMerchantUpgrade grants StaticBonusType.BazaarMedalSilver and consumes one item")
                .Given(ItemInInventoryWithEffectAndDays_, ItemEffectType.SilverNosMerchantUpgrade, 30)
                .WhenAsync(UsingTheItem)
                .Then(StaticBonusListShouldContain_, StaticBonusType.BazaarMedalSilver)
                .And(ItemStackCountShouldBe_, (short)0)
                .And(StaticBonusDateEndShouldNotBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GoldMerchantGrantsGoldBonusAndConsumesItem()
        {
            await new Spec("GoldNosMerchantUpgrade grants StaticBonusType.BazaarMedalGold")
                .Given(ItemInInventoryWithEffectAndDays_, ItemEffectType.GoldNosMerchantUpgrade, 30)
                .WhenAsync(UsingTheItem)
                .Then(StaticBonusListShouldContain_, StaticBonusType.BazaarMedalGold)
                .And(ItemStackCountShouldBe_, (short)0)
                .And(StaticBonusDateEndShouldNotBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DuplicateGoldBlocksSilverGrant()
        {
            await new Spec("Silver medal is rejected when character already holds a Gold medal (either variant blocks both)")
                .Given(ItemInInventoryWithEffectAndDays_, ItemEffectType.SilverNosMerchantUpgrade, 30)
                .And(CharacterAlreadyHasStaticBonus_, StaticBonusType.BazaarMedalGold)
                .WhenAsync(UsingTheItem)
                .Then(StaticBonusListShouldNotContain_, StaticBonusType.BazaarMedalSilver)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DuplicateSilverBlocksGoldGrant()
        {
            await new Spec("Gold medal is rejected when character already holds a Silver medal")
                .Given(ItemInInventoryWithEffectAndDays_, ItemEffectType.GoldNosMerchantUpgrade, 30)
                .And(CharacterAlreadyHasStaticBonus_, StaticBonusType.BazaarMedalSilver)
                .WhenAsync(UsingTheItem)
                .Then(StaticBonusListShouldNotContain_, StaticBonusType.BazaarMedalGold)
                .And(ItemStackCountShouldBe_, (short)1)
                .ExecuteAsync();
        }

        private void ItemInInventoryWithEffectAndDays_(ItemEffectType effect, int days)
        {
            var item = new Item
            {
                VNum = MedalVNum,
                Type = NoscorePocketType.Main,
                Effect = effect,
                EffectValue = days,
            };
            var inst = new ItemInstanceForTest(MedalVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void CharacterAlreadyHasStaticBonus_(StaticBonusType type) =>
            _session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = _session.Character.CharacterId,
                StaticBonusType = type,
            });

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

        private void StaticBonusCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.StaticBonusList.Count);

        private void StaticBonusListShouldContain_(StaticBonusType type) =>
            Assert.IsTrue(_session.Character.StaticBonusList.Any(b => b.StaticBonusType == type));

        private void StaticBonusListShouldNotContain_(StaticBonusType type) =>
            Assert.IsFalse(_session.Character.StaticBonusList.Any(b => b.StaticBonusType == type));

        private void StaticBonusDateEndShouldNotBeNull() =>
            Assert.IsNotNull(_session.Character.StaticBonusList.Last().DateEnd);

        private void ItemStackCountShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(MedalVNum));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
