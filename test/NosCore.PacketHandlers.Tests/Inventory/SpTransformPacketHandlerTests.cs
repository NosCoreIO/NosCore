//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.SpeedService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.TransformationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemInfoService;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.Algorithm.SpecialistPointService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable 618

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class SpTransformPacketHandlerTests
    {
        private IItemGenerationService Item = null!;
        private ClientSession Session = null!;
        private SpTransformPacketHandler SpTransformPacketHandler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Item = TestHelpers.Instance.GenerateItemProvider();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            ItemInfo.Setup(i => i.GenerateInfo(It.IsAny<NosCore.GameObject.Services.ItemGenerationService.Item.IItemInstance>()))
                .Returns(new NosCore.Packets.ServerPackets.Inventory.SlInfoPacket());
            SpTransformPacketHandler = new SpTransformPacketHandler(TestHelpers.Instance.Clock,
                new TransformationService(TestHelpers.Instance.Clock, new Mock<IExperienceService>().Object,
                    new Mock<IJobExperienceService>().Object, new Mock<IHeroExperienceService>().Object,
                    new Mock<ILogger<TransformationService>>().Object, TestHelpers.Instance.LogLanguageLocalizer, TestHelpers.Instance.WorldConfiguration,
                    new NosCore.GameObject.Services.SpeedCalculationService.SpeedCalculationService(new SpeedService()),
                    new Mock<NosCore.GameObject.Services.SkillService.ISkillService>().Object,
                    new Mock<NosCore.GameObject.Services.BattleService.IBattleStatsProvider>().Object),
                new SpecialistCardService(new SpecialistPointService()),
                ItemInfo.Object,
                new Mock<NosCore.GameObject.Services.BattleService.IBattleStatsProvider>().Object,
                new Mock<NosCore.GameObject.Services.BattleService.IVitalityService>().Object);
        }

        private readonly Mock<IItemInfoService> ItemInfo = new();

        [TestMethod]
        public async Task SpendingSkillPointsOnTheWornCardShouldKeepThem()
        {
            await new Spec("Spending skill points on the worn card should keep them")
                .Given(CharacterHasSpEquipped)
                .And(CharacterIsTransformed)
                .And(TheCardIsLevelForty)
                .WhenAsync(SpendingTenAttackAndFiveHpMp)
                .Then(TheCardShouldHaveTenAttackAndFiveHpMp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpendingMorePointsThanTheCardHasShouldChangeNothing()
        {
            await new Spec("Spending more points than the card has should change nothing")
                .Given(CharacterHasSpEquipped)
                .And(CharacterIsTransformed)
                .And(TheCardIsLevelForty)
                .WhenAsync(SpendingSixtyOneAttack)
                .Then(TheCardShouldHaveNoPointsSpent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpendingPointsWithoutBeingTransformedShouldBeRefused()
        {
            await new Spec("Spending points without being transformed should be refused")
                .Given(CharacterHasSpEquipped)
                .And(TheCardIsLevelForty)
                .WhenAsync(SpendingTenAttackAndFiveHpMp)
                .Then(TheCardShouldHaveNoPointsSpent)
                .And(ShouldReceiveCardMustBeEquippedMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpendingPointsOnAnotherCardShouldBeRefused()
        {
            await new Spec("Spending points for a card other than the worn one should be refused")
                .Given(CharacterHasSpEquipped)
                .And(CharacterIsTransformed)
                .And(TheCardIsLevelForty)
                .WhenAsync(SpendingPointsWithAnotherTransportId)
                .Then(TheCardShouldHaveNoPointsSpent)
                .ExecuteAsync();
        }

        private SpecialistInstance WornCard() =>
            (SpecialistInstance)Session.Character.InventoryService
                .LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)!.ItemInstance;

        private void TheCardIsLevelForty()
        {
            WornCard().SpLevel = 40;
        }

        private Task SpendingTenAttackAndFiveHpMp() =>
            SpTransformPacketHandler.ExecuteAsync(new SpTransformPacket
            {
                Type = SlPacketType.ChangePoints, TransportId = WornCard().TransportId,
                SpecialistDamage = 10, SpecialistDefense = 0, SpecialistElement = 0, SpecialistHP = 5
            }, Session);

        private Task SpendingSixtyOneAttack() =>
            SpTransformPacketHandler.ExecuteAsync(new SpTransformPacket
            {
                Type = SlPacketType.ChangePoints, TransportId = WornCard().TransportId,
                SpecialistDamage = 61, SpecialistDefense = 0, SpecialistElement = 0, SpecialistHP = 0
            }, Session);

        private Task SpendingPointsWithAnotherTransportId() =>
            SpTransformPacketHandler.ExecuteAsync(new SpTransformPacket
            {
                Type = SlPacketType.ChangePoints, TransportId = WornCard().TransportId + 1,
                SpecialistDamage = 10, SpecialistDefense = 0, SpecialistElement = 0, SpecialistHP = 0
            }, Session);

        private void TheCardShouldHaveTenAttackAndFiveHpMp()
        {
            Assert.AreEqual(10, WornCard().SlDamage);
            Assert.AreEqual(5, WornCard().SlHp);
            ItemInfo.Verify(i => i.GenerateInfo(WornCard()), Times.Once);
        }

        private void TheCardShouldHaveNoPointsSpent()
        {
            Assert.AreEqual(0, WornCard().SlDamage + WornCard().SlDefence + WornCard().SlElement + WornCard().SlHp);
        }

        private void ShouldReceiveCardMustBeEquippedMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(Game18NConstString.SpecialistCardMustBeEquipped, packet?.Message);
        }

        [TestMethod]
        public async Task TransformingWithoutSpShouldShowError()
        {
            await new Spec("Transforming without sp should show error")
                .WhenAsync(AttemptingToTransform)
                .Then(ShouldReceiveNoSpEquippedMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TransformingWhileInVehicleShouldFail()
        {
            await new Spec("Transforming while in vehicle should fail")
                .Given(CharacterIsInVehicle)
                .And(CharacterHasSpEquipped)
                .WhenAsync(AttemptingToTransform)
                .Then(ShouldReceiveCantUseInVehicleMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TransformingWhileSittingShouldBeIgnored()
        {
            await new Spec("Transforming while sitting should be ignored")
                .Given(CharacterIsSitting)
                .WhenAsync(AttemptingToTransform)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemovingSpWhileTransformedShouldUntransform()
        {
            await new Spec("Removing sp while transformed should untransform")
                .Given(CharacterHasSpEquipped)
                .And(CharacterIsTransformed)
                .WhenAsync(AttemptingToTransformWithWearAndTransform)
                .Then(CharacterShouldNotBeTransformed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TransformingWithSpPointsAndReputationShouldSucceed()
        {
            await new Spec("Transforming with sp points and reputation should succeed")
                .Given(CharacterHasSpPoints)
                .And(CharacterHasHighReputation)
                .And(CharacterHasSpEquipped)
                .AndAsync(TheTransformationBarHasRun)
                .WhenAsync(AttemptingToTransformWithWearAndTransform)
                .Then(CharacterShouldBeTransformed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TransformingWithoutTheBarShouldBeIgnored()
        {
            await new Spec("The bar's follow-up sent without the bar having run should not transform")
                .Given(CharacterHasSpPoints)
                .And(CharacterHasHighReputation)
                .And(CharacterHasSpEquipped)
                .WhenAsync(AttemptingToTransformWithWearAndTransform)
                .Then(CharacterShouldNotBeTransformed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TransformingBeforeTheBarEndsShouldBeIgnored()
        {
            await new Spec("The bar's follow-up sent before its five seconds have run should not transform")
                .Given(CharacterHasSpPoints)
                .And(CharacterHasHighReputation)
                .And(CharacterHasSpEquipped)
                .AndAsync(AttemptingToTransform)
                .WhenAsync(AttemptingToTransformWithWearAndTransform)
                .Then(CharacterShouldNotBeTransformed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TransformingWithBadFairyElementShouldFail()
        {
            await new Spec("Transforming with bad fairy element should fail")
                .Given(CharacterHasSpPoints)
                .And(CharacterHasHighReputation)
                .And(CharacterHasSpAndMismatchedFairyEquipped)
                .AndAsync(TheTransformationBarHasRun)
                .WhenAsync(AttemptingToTransformWithWearAndTransform)
                .Then(ShouldReceiveDifferentElementMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TransformingWithLowReputationShouldFail()
        {
            await new Spec("Transforming with low reputation should fail")
                .Given(CharacterHasSpPoints)
                .And(CharacterHasSpEquipped)
                .AndAsync(TheTransformationBarHasRun)
                .WhenAsync(AttemptingToTransformWithWearAndTransform)
                .Then(ShouldReceiveLowReputationMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TransformingDuringCooldownShouldShowCooldownMessage()
        {
            await new Spec("Transforming during cooldown should show cooldown message")
                .Given(CharacterHasSpPoints)
                .And(CharacterHasSpEquipped)
                .And(CharacterHasSpCooldown)
                .WhenAsync(AttemptingToTransformWithWearAndTransform)
                .Then(ShouldReceiveCooldownMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TransformingWithoutSpPointsShouldShowNoPointsMessage()
        {
            await new Spec("Transforming without sp points should show no points message")
                .Given(CharacterHasSpEquipped)
                .And(CharacterHasLastSpSet)
                .WhenAsync(AttemptingToTransformWithWearAndTransform)
                .Then(ShouldReceiveNoSpPointsMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TransformingShouldShowDelayPacket()
        {
            await new Spec("Transforming should show delay packet")
                .Given(CharacterHasSpPoints)
                .And(CharacterHasSpEquipped)
                .And(CharacterHasLastSpSet)
                .WhenAsync(AttemptingToTransform)
                .Then(ShouldReceiveDelayPacket)
                .ExecuteAsync();
        }

        private void CharacterIsInVehicle()
        {
            Session.Character.IsVehicled = true;
        }

        private void CharacterHasSpEquipped()
        {
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(912, 1), Session.Character.CharacterId));
            var item = Session.Character.InventoryService.First();
            item.Value.Type = NoscorePocketType.Wear;
            item.Value.Slot = (byte)EquipmentType.Sp;
        }

        private void CharacterIsSitting()
        {
            Session.Character.IsSitting = true;
        }

        private void CharacterIsTransformed()
        {
            Session.Character.UseSp = true;
        }

        private void CharacterHasSpPoints()
        {
            Session.Character.SpPoint = 1;
        }

        private void CharacterHasHighReputation()
        {
            Session.Character.Reput = 5000000;
        }

        private void CharacterHasSpAndMismatchedFairyEquipped()
        {
            var spItem = Session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(Item.Create(912, 1), Session.Character.CharacterId))!
                .First();
            var fairy = Session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(Item.Create(2, 1), Session.Character.CharacterId))!
                .First();

            spItem.Type = NoscorePocketType.Wear;
            spItem.Slot = (byte)EquipmentType.Sp;
            fairy.Type = NoscorePocketType.Wear;
            fairy.Slot = (byte)EquipmentType.Fairy;
        }

        private void CharacterHasSpCooldown()
        {
            Session.Character.LastSp = TestHelpers.Instance.Clock.GetCurrentInstant();
            Session.Character.SpCooldown = 30;
        }

        private void CharacterHasLastSpSet()
        {
            Session.Character.LastSp = TestHelpers.Instance.Clock.GetCurrentInstant();
        }

        private async Task AttemptingToTransform()
        {
            await SpTransformPacketHandler.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSp }, Session);
        }

        private async Task TheTransformationBarHasRun()
        {
            await AttemptingToTransform();
            TestHelpers.Instance.Clock.AdvanceSeconds(5);
            Session.LastPackets.Clear();
        }

        private async Task AttemptingToTransformWithWearAndTransform()
        {
            await SpTransformPacketHandler.ExecuteAsync(new SpTransformPacket { Type = SlPacketType.WearSpAndTransform }, Session);
        }

        private void ShouldReceiveNoSpEquippedMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Type == MessageType.Default && packet?.Message == Game18NConstString.NoSpecialistCardEquipped);
        }

        private void ShouldReceiveCantUseInVehicleMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.CantUseInVehicle);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.IsNull(Session.LastPackets.FirstOrDefault());
        }

        private void CharacterShouldNotBeTransformed()
        {
            Assert.IsFalse(Session.Character.UseSp);
        }

        private void CharacterShouldBeTransformed()
        {
            Assert.IsTrue(Session.Character.UseSp);
        }

        private void ShouldReceiveDifferentElementMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.SpecialistAndFairyDifferentElement);
        }

        private void ShouldReceiveLowReputationMessage()
        {
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player &&
                packet?.VisualId == Session.Character.CharacterId &&
                packet?.Type == SayColorType.Yellow &&
                packet?.Message == Game18NConstString.CanNotBeWornReputationLow);
        }

        private void ShouldReceiveCooldownMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Type == MessageType.Default &&
                packet?.Message == Game18NConstString.CantTrasformWithSideEffect &&
                packet?.ArgumentType == 4 &&
                (short?)packet?.Game18NArguments[0] == 30);
        }

        private void ShouldReceiveNoSpPointsMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Type == MessageType.Default && packet?.Message == Game18NConstString.ZeroPointsTransformationCancelled);
        }

        private void ShouldReceiveDelayPacket()
        {
            var packet = (DelayPacket?)Session.LastPackets.FirstOrDefault(s => s is DelayPacket);
            Assert.IsTrue(packet?.Delay == 5000);
        }
    }
}
