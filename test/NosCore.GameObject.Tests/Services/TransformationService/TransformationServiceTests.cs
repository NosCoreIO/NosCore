//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.Algorithm.SpeedService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Linq;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.TransformationService
{
    [TestClass]
    public class TransformationServiceTests
    {
        private TestWorld World;
        private static readonly ILogger<NosCore.GameObject.Services.TransformationService.TransformationService> Logger = new Mock<ILogger<NosCore.GameObject.Services.TransformationService.TransformationService>>().Object;
        private ITransformationService Service;
        private NosCore.GameObject.Services.BattleService.BuffService Buffs;
        private Mock<ISpecialistWingsService> Wings = new();
        private ClientSession Session;
        private Mock<ISessionGroup> Onlookers = new();
        private const short BoostCardId = 336;
        private const short MountWithABuff = 13752;
        private const short RidingCardId = 4328;
        private const short BoostedRidingCardId = 4329;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.MapInstance = World.GetBaseMap(1);

            var cards = World.Freeze<Mock<NosCore.GameObject.Services.BattleService.ICardCatalog>>();
            Buffs = World.Create<NosCore.GameObject.Services.BattleService.BuffService>();
            World.Inject<NosCore.GameObject.Services.BattleService.IBuffService>(Buffs);
            cards.Setup(c => c.IsGrantedBy(BoostCardId, NosCore.Data.Enumerations.Items.ItemEffectType.SpeedBooster)).Returns(true);
            var ridingCard = new NosCore.Data.StaticEntities.CardDto { CardId = RidingCardId, GroupId = RidingCardId, Duration = 30000 };
            cards.Setup(c => c.GetItemCard(MountWithABuff)).Returns(ridingCard);
            cards.Setup(c => c.GetCard(RidingCardId)).Returns(ridingCard);
            cards.Setup(c => c.GetCard(BoostedRidingCardId)).Returns(new NosCore.Data.StaticEntities.CardDto
            {
                CardId = BoostedRidingCardId, GroupId = RidingCardId, Duration = 150
            });
            cards.Setup(c => c.GetCardBCards(RidingCardId)).Returns(new List<NosCore.Data.StaticEntities.BCardDto>());
            Service = new GameObject.Services.TransformationService.TransformationService(
                World.Clock,
                new Mock<IExperienceService>().Object,
                new Mock<IJobExperienceService>().Object,
                new Mock<IHeroExperienceService>().Object, new NosCore.Algorithm.SpExperienceService.SpExperienceService(), new NosCore.Algorithm.SkillPointService.SkillPointService(),
                Logger,
                World.LogLanguageLocalizer,
                World.WorldConfiguration,
                World.Create<GameObject.Services.SpeedCalculationService.SpeedCalculationService>(),
                new Mock<GameObject.Services.SkillService.ISkillService>().Object,
                new Mock<NosCore.GameObject.Services.BattleService.IBattleStatsProvider>().Object,
                Buffs,
                cards.Object,
                Wings.Object);
        }

        [TestMethod]
        public async Task RemovingSpShouldResetMorphValues()
        {
            await new Spec("Removing SP should reset morph values")
                .Given(CharacterHasSpEquipped)
                .WhenAsync(RemovingSp)
                .Then(MorphShouldBeReset)
                .And(SpCooldownShouldBeSet)
                .And(TheWingsShouldBeRefreshed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACardWearingASkinTransformsIntoTheSkin()
        {
            await new Spec("Transforming with a card that wears a skin shows the skin's look, not the card's")
                .Given(TheChefCardWearingTheMasterchefSkinIsWorn)
                .WhenAsync(Transforming)
                .Then(TheMapIsSentTheMorph_, MasterchefLook)
                .And(CharacterMorphShouldBe_, MasterchefLook)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlookersSeeTheTransformationFinish()
        {
            await new Spec("The map sees the transformed player's gear and the animation that ends the transformation")
                .Given(TheChefCardWearingTheMasterchefSkinIsWorn)
                .WhenAsync(Transforming)
                .Then(TheMapIsSentTheCharactersGear)
                .And(TheMapIsSentTheTransformationFinish)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GettingOffAMountShowsTheSkinAgain()
        {
            await new Spec("A transformed player who gets off a mount takes the skin's look back")
                .Given(TheChefCardWearingTheMasterchefSkinIsWorn)
                .AndAsync(Transforming)
                .AndAsync(CharacterIsVehicled)
                .WhenAsync(RemovingVehicle)
                .Then(CharacterMorphShouldBe_, MasterchefLook)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingVehicleShouldSetVehicledState()
        {
            await new Spec("Changing vehicle should set vehicled state")
                .WhenAsync(ChangingToVehicle)
                .Then(CharacterShouldBeVehicled)
                .And(VehicleSpeedShouldBeSet)
                .And(TheSpeedTheClientSeesIsTheVehicles)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DismountingEndsTheSpeedBoost()
        {
            await new Spec("Dismounting ends the speed boost, so the next ride starts at the mount's own speed")
                .GivenAsync(CharacterIsVehicled)
                .AndAsync(TheMountIsBoosted)
                .WhenAsync(RemovingVehicle)
                .AndAsync(CharacterIsVehicled)
                .Then(TheBoostIsGone)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemovingVehicleShouldResetState()
        {
            await new Spec("Removing vehicle should reset state")
                .GivenAsync(CharacterIsVehicled)
                .WhenAsync(RemovingVehicle)
                .Then(CharacterShouldNotBeVehicled)
                .And(VehicleSpeedShouldBeZero)
                .And(TheSpeedTheClientSeesIsBackOnFoot)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheTeamLeavesTheMapWhileItsOwnerRides()
        {
            var pet = await TeamPetOnTheMapAsync();

            await ChangingToVehicle();

            Assert.IsNull(pet.Entity);
        }

        [TestMethod]
        public async Task TheTeamComesBackWhenItsOwnerGetsOff()
        {
            var pet = await TeamPetOnTheMapAsync();
            await ChangingToVehicle();

            await RemovingVehicle();

            Assert.IsNotNull(pet.Entity);
        }

        private async Task<NosCore.GameObject.Services.MateService.Mate> TeamPetOnTheMapAsync()
        {
            var pet = TestMates.Pet(Session.Character.CharacterId);
            Session.Character.Mates[pet.MateTransportId] = pet;
            await pet.SpawnAsync(Session.Character, Session.Character.MapInstance,
                Session.Character.PositionX, Session.Character.PositionY);
            return pet;
        }

        // VehicleSpeed alone changes nothing the player can feel: `cond` carries Speed, and Speed
        // was written once at login from the class table. Asserting only VehicleSpeed is what let
        // a mount that adds no speed look correct.
        private void TheSpeedTheClientSeesIsTheVehicles()
        {
            Assert.AreEqual(20, Session.Character.Speed);
            Assert.AreEqual(20, Session.Character.GenerateCond().Speed);
        }

        private void TheSpeedTheClientSeesIsBackOnFoot()
        {
            Assert.AreEqual(new SpeedService().GetSpeed(Session.Character.Class),
                Session.Character.Speed);
        }

        private const short MasterchefLook = 37;

        private void TheChefCardWearingTheMasterchefSkinIsWorn()
        {
            var card = new SpecialistInstance(new GameObject.Services.ItemGenerationService.Item.Item
            {
                VNum = 4575, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist, Morph = 38
            }) { Skin = MasterchefLook, SpLevel = 1 };
            var worn = InventoryItemInstance.Create(card, Session.Character.CharacterId);
            worn.Type = NoscorePocketType.Wear;
            worn.Slot = (short)EquipmentType.Sp;
            Session.Character.InventoryService[worn.ItemInstanceId] = worn;
            Session.Character.MapInstance.Sessions = Onlookers.Object;
        }

        private Task Transforming() => Service.ChangeSpAsync(Session);

        private void CharacterMorphShouldBe_(short morph) => Assert.AreEqual(morph, Session.Character.Morph);

        private void TheMapIsSentTheMorph_(short morph) =>
            Assert.AreEqual(morph, Onlookers.Invocations.SelectMany(i => i.Arguments)
                .SelectMany(a => a is IEnumerable<IPacket> many ? many : a is IPacket one ? new[] { one } : [])
                .OfType<CModePacket>().Last().Morph);

        private IEnumerable<IPacket> SentToTheMap() => Onlookers.Invocations.SelectMany(i => i.Arguments)
            .SelectMany(a => a is IEnumerable<IPacket> many ? many : a is IPacket one ? new[] { one } : []);

        private void TheMapIsSentTheCharactersGear() =>
            PacketAssert.AreEqual(Session.Character.GenerateEq(), SentToTheMap().OfType<EqPacket>().Single());

        private void TheMapIsSentTheTransformationFinish() =>
            PacketAssert.AreEqual(new GuriPacket
            {
                Type = GuriPacketType.CharacterAnimation,
                Argument = (uint)VisualType.Player,
                EntityId = Session.Character.CharacterId,
                Value = 0,
                SecondValue = 0
            }, SentToTheMap().OfType<GuriPacket>().Single());

        private void CharacterHasSpEquipped()
        {
            Session.Character.UseSp = true;
            Session.Character.Morph = 100;
            Session.Character.MorphUpgrade = 10;
            Session.Character.MorphDesign = 5;
        }

        [TestMethod]
        public async Task AMountWithARidingBuffGivesItWhileRidden()
        {
            await Riding(MountWithABuff);
            Assert.IsTrue(Buffs.HasBuff(Session.Character, RidingCardId));

            await Service.RemoveVehicleAsync(Session);
            Assert.IsFalse(Buffs.HasBuff(Session.Character, RidingCardId));
        }

        [TestMethod]
        public async Task DismountingEndsTheBuffABoostSwappedTheRidingBuffFor()
        {
            await Riding(MountWithABuff);
            await Buffs.ApplyAsync(Session.Character, new NosCore.Data.StaticEntities.CardDto
            {
                CardId = BoostedRidingCardId, GroupId = RidingCardId, Duration = 150
            }, new List<NosCore.Data.StaticEntities.BCardDto>(), Session.Character);
            Assert.IsFalse(Buffs.HasBuff(Session.Character, RidingCardId));

            await Service.RemoveVehicleAsync(Session);
            Assert.AreEqual(0, Buffs.GetActiveBuffs(Session.Character).Count);
        }

        [TestMethod]
        public async Task AMountWithoutARidingBuffGivesNone()
        {
            await Riding(5196);

            Assert.AreEqual(0, Buffs.GetActiveBuffs(Session.Character).Count);
        }

        private Task Riding(short vnum)
        {
            var mount = new GameObject.Services.ItemGenerationService.Item.Item { VNum = vnum, Speed = 20, Morph = 2432 };
            return Service.ChangeVehicleAsync(Session, mount, mount);
        }

        private async Task RemovingSp()
        {
            await Service.RemoveSpAsync(Session);
        }

        private async Task ChangingToVehicle()
        {
            var vehicleItem = new GameObject.Services.ItemGenerationService.Item.Item
            {
                VNum = 5196,
                Speed = 20,
                Morph = 2432,
                SecondMorph = 0
            };
            await Service.ChangeVehicleAsync(Session, vehicleItem, vehicleItem);
        }

        private async Task CharacterIsVehicled()
        {
            var vehicleItem = new GameObject.Services.ItemGenerationService.Item.Item
            {
                VNum = 5196,
                Speed = 20,
                SpeedBoost = 3,
                SpeedBoostDuration = 5,
                Morph = 2432,
                SecondMorph = 0
            };
            World.ItemList.RemoveAll(i => i.VNum == vehicleItem.VNum);
            World.ItemList.Add(vehicleItem);
            await Service.ChangeVehicleAsync(Session, vehicleItem, vehicleItem);
        }

        private async Task TheMountIsBoosted()
        {
            await Buffs.ApplyAsync(Session.Character,
                new NosCore.Data.StaticEntities.CardDto { CardId = BoostCardId },
                new List<NosCore.Data.StaticEntities.BCardDto>(), Session.Character, 5000);
        }

        private void TheBoostIsGone()
        {
            Assert.IsFalse(Buffs.HasBuff(Session.Character, BoostCardId));
            Assert.AreEqual(20, Session.Character.Speed);
        }

        private async Task RemovingVehicle()
        {
            await Service.RemoveVehicleAsync(Session);
        }

        private void MorphShouldBeReset()
        {
            Assert.AreEqual(0, Session.Character.Morph);
            Assert.AreEqual(0, Session.Character.MorphUpgrade);
            Assert.AreEqual(0, Session.Character.MorphDesign);
        }

        private void SpCooldownShouldBeSet()
        {
            Assert.AreEqual(30, Session.Character.SpCooldown);
            Assert.IsFalse(Session.Character.UseSp);
        }

        private void CharacterShouldBeVehicled()
        {
            Assert.IsTrue(Session.Character.IsVehicled);
        }

        private void VehicleSpeedShouldBeSet()
        {
            Assert.AreEqual((byte)20, Session.Character.VehicleSpeed);
        }

        private void CharacterShouldNotBeVehicled()
        {
            Assert.IsFalse(Session.Character.IsVehicled);
        }

        private void VehicleSpeedShouldBeZero()
        {
            Assert.AreEqual((byte)0, Session.Character.VehicleSpeed);
        }

        private void TheWingsShouldBeRefreshed()
        {
            Wings.Verify(w => w.RefreshAsync(It.IsAny<NosCore.GameObject.Ecs.PlayerComponentBundle>()), Times.Once);
        }
    }
}
