//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class CuarryBankVipMedalHandlerTests
    {
        private const short MedalVNum = 5838;
        private ClientSession _session = null!;
        private CuarryBankVipMedalHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _handler = new CuarryBankVipMedalHandler(TestHelpers.Instance.Clock);
        }

        [TestMethod]
        public async Task UnrelatedEffectIsIgnored()
        {
            await new Spec("An item without the bank VIP effect grants nothing and is not consumed")
                .Given(MedalInInventoryWithEffectAndDays_, ItemEffectType.Teleport, 10)
                .WhenAsync(UsingTheMedal)
                .Then(CharacterShouldNotHaveBankVip)
                .And(MedalCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MedalGrantsBankVipForItsDaysAndIsConsumed()
        {
            await new Spec("The VIP medal grants Cuarry Bank VIP until now plus its days and is consumed")
                .Given(MedalInInventoryWithEffectAndDays_, ItemEffectType.CuarryBankVipMedal, 10)
                .WhenAsync(UsingTheMedal)
                .Then(BankVipShouldEndIn_Days, 10)
                .And(MedalCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MedalIsKeptWhileVipIsActive()
        {
            await new Spec("A second medal is not used while the character already has Cuarry Bank VIP")
                .Given(MedalInInventoryWithEffectAndDays_, ItemEffectType.CuarryBankVipMedal, 30)
                .And(CharacterAlreadyHasBankVip)
                .WhenAsync(UsingTheMedal)
                .Then(BankVipCountShouldBe_, 1)
                .And(MedalCountShouldBe_, 1)
                .ExecuteAsync();
        }

        private void MedalInInventoryWithEffectAndDays_(ItemEffectType effect, int days)
        {
            var item = new Item { VNum = MedalVNum, Type = NoscorePocketType.Main, Effect = effect, EffectValue = days };
            var inst = new ItemInstanceForTest(MedalVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void CharacterAlreadyHasBankVip() =>
            _session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = _session.Character.CharacterId,
                StaticBonusType = StaticBonusType.CuarryBankVip,
            });

        private Task UsingTheMedal() =>
            _handler.Handle(new ItemUsedEvent(_session, _item, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 1,
            }));

        private void CharacterShouldNotHaveBankVip() => BankVipCountShouldBe_(0);

        private void BankVipCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.StaticBonusList.Count(b => b.StaticBonusType == StaticBonusType.CuarryBankVip));

        private void BankVipShouldEndIn_Days(int days)
        {
            var bonus = _session.Character.StaticBonusList.Single(b => b.StaticBonusType == StaticBonusType.CuarryBankVip);
            Assert.AreEqual(TestHelpers.Instance.Clock.GetCurrentInstant().Plus(Duration.FromDays(days)), bonus.DateEnd);
        }

        private void MedalCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(MedalVNum));

        private sealed class ItemInstanceForTest(short vnum) : ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
