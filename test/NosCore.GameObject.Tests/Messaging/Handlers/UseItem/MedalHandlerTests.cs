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
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class MedalHandlerTests
    {
        private const short MedalVNum = 1115;
        private TestWorld World;
        private ClientSession Session;
        private InventoryItemInstance Medal;
        private byte Mode = 1;
        private Instant EndBefore;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public async Task AMedalOfEreniaAddsEightSlotsToEachBag()
        {
            await new Spec("A Medal of Erenia starts a 30 day bonus, is used up and adds 8 slots to the three bags")
                .Given(AMedal_, ItemEffectType.MedalOfErenia)
                .WhenAsync(UsingIt)
                .Then(TheBonusShouldBe_, StaticBonusType.MedalOfErenia)
                .And(TheBonusShouldEndInDays_, 30)
                .And(TheMedalShouldBeUsedUp)
                .And(EachBagShouldGain_, 8)
                .And(TheMessageShouldBe_, Game18NConstString.EreniaMedalActivated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAdventurersMedalAddsFourSlotsToEachBag()
        {
            await new Spec("An Adventurer's Medal adds 4 slots to the three bags")
                .Given(AMedal_, ItemEffectType.AdventurersMedal)
                .WhenAsync(UsingIt)
                .Then(TheBonusShouldBe_, StaticBonusType.AdventurersMedal)
                .And(EachBagShouldGain_, 4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMedalIsRefusedWithTheKnapsack()
        {
            await new Spec("A Medal of Erenia is refused while the Adventurer's Knapsack is active")
                .Given(AMedal_, ItemEffectType.MedalOfErenia)
                .And(TheCharacterHas_, StaticBonusType.BackPack)
                .WhenAsync(UsingIt)
                .Then(TheMessageShouldBe_, Game18NConstString.NotWithKnapsack)
                .And(TheMedalShouldStillBeThere)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheTwoSlotMedalsAreRefusedTogether()
        {
            await new Spec("A Medal of Erenia is refused while an Adventurer's Medal is active")
                .Given(AMedal_, ItemEffectType.MedalOfErenia)
                .And(TheCharacterHas_, StaticBonusType.AdventurersMedal)
                .WhenAsync(UsingIt)
                .Then(TheMessageShouldBe_, Game18NConstString.NoMedalEreniaAndAdventurerTogether)
                .And(TheMedalShouldStillBeThere)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingAnActiveMedalAgainAsksFirst()
        {
            await new Spec("Using a medal that is already active asks whether to extend it, and keeps the medal")
                .Given(AMedal_, ItemEffectType.MimisMedal)
                .And(TheCharacterHas_, StaticBonusType.MimisMedal)
                .And(TheClientAsksFirst)
                .WhenAsync(UsingIt)
                .Then(TheQuestionShouldBe_, Game18NConstString.AskExtendMedal)
                .And(TheMedalShouldStillBeThere)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingExtendsTheMedal()
        {
            await new Spec("Confirming adds the medal's days to the running bonus and uses the medal up")
                .Given(AMedal_, ItemEffectType.MimisMedal)
                .And(TheCharacterHas_, StaticBonusType.MimisMedal)
                .WhenAsync(UsingIt)
                .Then(TheBonusShouldEndLaterBy_, 3)
                .And(TheMedalShouldBeUsedUp)
                .And(TheMessageShouldBe_, Game18NConstString.MedalUsablePeriodChanged)
                .ExecuteAsync();
        }

        private void AMedal_(ItemEffectType effect)
        {
            var days = effect switch { ItemEffectType.MimisMedal => 3, ItemEffectType.MedalOfFriendship => 10, _ => 30 };
            var item = new Item { VNum = MedalVNum, Type = NoscorePocketType.Main, ItemType = ItemType.Special, Effect = effect, EffectValue = days };
            Medal = InventoryItemInstance.Create(new MedalInstance(MedalVNum) { Amount = 1, Item = item }, Session.Character.CharacterId);
            Medal.Slot = 0;
            Medal.Type = NoscorePocketType.Main;
            Session.Character.InventoryService[Medal.ItemInstanceId] = Medal;
        }

        private void TheCharacterHas_(StaticBonusType type)
        {
            var bonus = new StaticBonusDto
            {
                CharacterId = Session.Character.CharacterId,
                StaticBonusType = type,
                DateEnd = World.Clock.GetCurrentInstant().Plus(Duration.FromDays(1)),
            };
            Session.Character.StaticBonusList.Add(bonus);
            EndBefore = bonus.DateEnd.Value;
        }

        private void TheClientAsksFirst() => Mode = 0;

        private async Task UsingIt() => await World.Create<MedalHandler>().Handle(new ItemUsedEvent(Session, Medal, new UseItemPacket
        {
            VisualType = VisualType.Player,
            VisualId = Session.Character.CharacterId,
            Type = PocketType.Main,
            Slot = 0,
            Mode = Mode,
        }));

        private void TheBonusShouldBe_(StaticBonusType type) =>
            Assert.AreEqual(type, Session.Character.StaticBonusList.Single().StaticBonusType);

        private void TheBonusShouldEndInDays_(int days) =>
            Assert.AreEqual(World.Clock.GetCurrentInstant().Plus(Duration.FromDays(days)), Session.Character.StaticBonusList.Single().DateEnd);

        private void TheBonusShouldEndLaterBy_(int days) =>
            Assert.AreEqual(EndBefore.Plus(Duration.FromDays(days)), Session.Character.StaticBonusList.Single().DateEnd);

        private void TheMedalShouldBeUsedUp() => Assert.AreEqual(0, Session.Character.InventoryService.CountItem(MedalVNum));

        private void TheMedalShouldStillBeThere() => Assert.AreEqual(1, Session.Character.InventoryService.CountItem(MedalVNum));

        private void EachBagShouldGain_(int slots)
        {
            var exts = Session.LastPackets.OfType<ExtsPacket>().Last();
            var size = World.WorldConfiguration.Value.BackpackSize;
            Assert.AreEqual((size + slots, size + slots, size + slots), (exts.MainExtension, exts.EquipmentExtension, exts.EtcExtension));
        }

        private void TheMessageShouldBe_(Game18NConstString message) =>
            Assert.IsTrue(Session.LastPackets.OfType<MsgiPacket>().Any(p => p.Message == message));

        private void TheQuestionShouldBe_(Game18NConstString question) =>
            Assert.AreEqual(question, Session.LastPackets.OfType<QnaiPacket>().Single().Question);

        private sealed class MedalInstance(short vnum) : ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
