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
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
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
    public class FamilyExperienceBoosterHandlerTests
    {
        private const short BoosterVNum = 5740;
        private TestWorld World;
        private ClientSession _session;
        private FamilyExperienceBoosterHandler _handler;
        private InventoryItemInstance _item;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _handler = World.Create<FamilyExperienceBoosterHandler>();
        }

        [TestMethod]
        public async Task ABoosterStartsTheBonusForItsDays()
        {
            await new Spec("A 7-day FXP Booster gives the character the bonus for 7 days and is used up")
                .Given(ABoosterOf_DaysInTheBag, 7)
                .WhenAsync(UsingTheBooster)
                .Then(TheBonusShouldEndIn_Days, 7)
                .And(TheBoosterShouldBeUsedUp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASecondBoosterAddsItsDays()
        {
            await new Spec("A 7-day booster used while 3 days are left leaves 10 days")
                .Given(ABoosterOf_DaysInTheBag, 7)
                .And(TheBonusAlreadyRunsFor_Days, 3)
                .WhenAsync(UsingTheBooster)
                .Then(TheBonusShouldEndIn_Days, 10)
                .And(TheCharacterShouldHaveOneBonus)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherItemIsIgnored()
        {
            await new Spec("An item with another effect gives no bonus")
                .Given(ABoosterOf_DaysInTheBag, 7)
                .And(TheItemHasAnotherEffect)
                .WhenAsync(UsingTheBooster)
                .Then(TheCharacterShouldHaveNoBonus)
                .ExecuteAsync();
        }

        private void ABoosterOf_DaysInTheBag(int days)
        {
            var item = new Item
            {
                VNum = BoosterVNum,
                Type = NoscorePocketType.Main,
                Effect = ItemEffectType.FamilyExperienceBooster,
                EffectValue = days,
            };
            var instance = new ItemInstanceForTest(BoosterVNum) { Amount = 1, Item = item };
            _item = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void TheItemHasAnotherEffect() => _item.ItemInstance.Item.Effect = ItemEffectType.Teleport;

        private void TheBonusAlreadyRunsFor_Days(int days) =>
            _session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = _session.Character.CharacterId,
                StaticBonusType = StaticBonusType.FamilyExperienceBooster,
                DateEnd = World.Clock.GetCurrentInstant().Plus(Duration.FromDays(days))
            });

        private Task UsingTheBooster() =>
            _handler.Handle(new ItemUsedEvent(_session, _item, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 1,
            }));

        private void TheBonusShouldEndIn_Days(int days) =>
            Assert.AreEqual(World.Clock.GetCurrentInstant().Plus(Duration.FromDays(days)),
                _session.Character.StaticBonusList.Single(b => b.StaticBonusType == StaticBonusType.FamilyExperienceBooster).DateEnd);

        private void TheCharacterShouldHaveOneBonus()
        {
            Assert.AreEqual(1, _session.Character.StaticBonusList.Count);
            Assert.IsTrue(_session.Character.HasFamilyExperienceBooster());
        }

        private void TheCharacterShouldHaveNoBonus() =>
            Assert.IsFalse(_session.Character.HasFamilyExperienceBooster());

        private void TheBoosterShouldBeUsedUp() =>
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(BoosterVNum));

        private sealed class ItemInstanceForTest(short vnum) : ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
