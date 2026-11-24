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
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.GameObject.Services.SkillService;
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
        private Mock<GameObject.Services.SkillService.ISkillService> SkillBar = new();
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
                SkillBar.Object,
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
        public async Task TheTransformingPlayerGetsTheirLinesInTheGamesOrder()
        {
            await new Spec("The transforming player gets their state, the look, the skills, the points, the level, then the finish")
                .Given(TheChefCardWearingTheMasterchefSkinIsWorn)
                .And(ThePlayerSeesWhatTheMapIsSent)
                .WhenAsync(Transforming)
                .Then(ThePlayerIsSentTheTransformationLinesInOrder)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheTransformingPlayerIsNotSentTheirOwnGear()
        {
            await new Spec("Only the others on the map are sent the transformed player's gear")
                .Given(TheChefCardWearingTheMasterchefSkinIsWorn)
                .And(ThePlayerSeesWhatTheMapIsSent)
                .WhenAsync(Transforming)
                .Then(TheMapIsSentTheCharactersGear)
                .And(ThePlayerIsSentNoGear)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TransformingEndsTheCardsThePlayerIsUnder()
        {
            await new Spec("Transforming ends the cards the player is under first, silently, at the player's own level")
                .Given(TheChefCardWearingTheMasterchefSkinIsWorn)
                .And(ThePlayerIsAtLevel_, 76)
                .AndAsync(ThePlayerIsUnderACardFromAMateAtLevel_, 71)
                .And(ThePlayerSeesWhatTheMapIsSent)
                .WhenAsync(Transforming)
                .Then(TheCardIsTakenDownBeforeTheTransformationLines)
                .And(ThePlayerIsNoLongerUnderTheCard)
                .And(TheEndOfTheCardIsNotAnnounced)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APermanentCardOutlivesTheTransformation()
        {
            await new Spec("A permanent card the player holds is kept through the transformation")
                .Given(TheChefCardWearingTheMasterchefSkinIsWorn)
                .AndAsync(ThePlayerHoldsAPermanentCard)
                .WhenAsync(Transforming)
                .Then(ThePlayerStillHoldsThePermanentCard)
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

        private const short YakWarriorLook = 1446;

        [TestMethod]
        public async Task TheMapSeesThePlayerTakeAMonsterForm()
        {
            Session.Character.MapInstance.Sessions = Onlookers.Object;

            await Service.ChangeMonsterFormAsync(Session, YakWarriorLook);

            Assert.IsTrue(Session.Character.InMonsterForm);
            CharacterMorphShouldBe_(YakWarriorLook);
            TheMapIsSentTheMorph_(YakWarriorLook);
        }

        [TestMethod]
        public async Task TheMapSeesThePlayerLeaveTheMonsterForm()
        {
            Session.Character.MapInstance.Sessions = Onlookers.Object;
            await Service.ChangeMonsterFormAsync(Session, YakWarriorLook);

            await Service.RemoveMonsterFormAsync(Session);

            Assert.IsFalse(Session.Character.InMonsterForm);
            CharacterMorphShouldBe_(0);
            TheMapIsSentTheMorph_(0);
        }

        [TestMethod]
        public async Task TransformingIntoASpecialistEndsTheMonsterForm()
        {
            TheChefCardWearingTheMasterchefSkinIsWorn();
            Session.Character.InMonsterForm = true;

            await Transforming();

            Assert.IsFalse(Session.Character.InMonsterForm);
            CharacterMorphShouldBe_(MasterchefLook);
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

        // The player is on the map too, so the map's lines reach them in the same stream as their own.
        private void ThePlayerSeesWhatTheMapIsSent()
        {
            Onlookers.Setup(s => s.Broadcast(It.IsAny<IPacket[]>()))
                .Callback<IPacket[]>(Receive).Returns(Task.CompletedTask);
            Onlookers.Setup(s => s.Broadcast(It.IsAny<IPacket[]>(), It.IsAny<ISessionMatcher>()))
                .Callback<IPacket[], ISessionMatcher>((packets, matcher) =>
                {
                    if (matcher.Matches(Session.Channel.NotNull().Id))
                    {
                        Receive(packets);
                    }
                }).Returns(Task.CompletedTask);
            SkillBar.Setup(s => s.LoadSpecialistSkillsAsync(It.IsAny<NosCore.GameObject.Ecs.PlayerComponentBundle>(), It.IsAny<short>(), It.IsAny<byte>()))
                .Returns(() => Session.Character.SendPacketAsync(Session.Character.GenerateSki(true)));

            void Receive(IPacket[] packets)
            {
                foreach (var packet in packets)
                {
                    Session.LastPackets.Enqueue(packet);
                }
            }
        }

        private void ThePlayerIsSentTheTransformationLinesInOrder()
        {
            CollectionAssert.AreEqual(new[]
            {
                typeof(CondPacket), typeof(ScPacket), typeof(StatPacket), typeof(CModePacket), typeof(SkiPacket),
                typeof(SpPacket), typeof(LevPacket), typeof(EffectPacket), typeof(GuriPacket)
            }, TransformationLinesReceived());
            PacketAssert.AreEqual(Session.Character.GenerateEff(196), Session.LastPackets.OfType<EffectPacket>().Single());
        }

        private List<System.Type> TransformationLinesReceived() => Session.LastPackets
            .OfType<IPacket>()
            .Where(p => p is BfPacket or CondPacket or ScPacket or StatPacket or CModePacket or SkiPacket or SpPacket
                or LevPacket or EqPacket or EffectPacket or GuriPacket)
            .Select(p => p.GetType())
            .ToList();

        private void ThePlayerIsSentNoGear() => Assert.IsFalse(Session.LastPackets.OfType<EqPacket>().Any());

        private const short MateBlessingCardId = 840;

        private void ThePlayerIsAtLevel_(int level) => Session.Character.Level = (byte)level;

        private async Task ThePlayerIsUnderACardFromAMateAtLevel_(int level)
        {
            var mate = await World.CreateSessionAsync();
            mate.Character.Level = (byte)level;
            await Buffs.ApplyAsync(Session.Character, new NosCore.Data.StaticEntities.CardDto { CardId = MateBlessingCardId, Duration = 3000 },
                new List<NosCore.Data.StaticEntities.BCardDto>(), mate.Character);
            Session.LastPackets.Clear();
        }

        private void TheCardIsTakenDownBeforeTheTransformationLines()
        {
            CollectionAssert.AreEqual(new[]
            {
                typeof(BfPacket), typeof(CondPacket), typeof(ScPacket), typeof(StatPacket), typeof(CModePacket), typeof(SkiPacket),
                typeof(SpPacket), typeof(LevPacket), typeof(EffectPacket), typeof(GuriPacket)
            }, TransformationLinesReceived());
            PacketAssert.AreEqual(new BfPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.VisualId,
                Buff = new BfPacket.BuffElementSubPacket { ChargeValue = 0, BuffId = MateBlessingCardId, Duration = 0 },
                BuffLevel = 76,
            }, Session.LastPackets.OfType<BfPacket>().Single());
        }

        private void ThePlayerIsNoLongerUnderTheCard() => Assert.IsFalse(Buffs.HasBuff(Session.Character, MateBlessingCardId));

        private void TheEndOfTheCardIsNotAnnounced() => Assert.IsFalse(Session.LastPackets.OfType<SayiPacket>().Any());

        private const short PermanentCardId = 999;

        private Task ThePlayerHoldsAPermanentCard() =>
            Buffs.ApplyPermanentAsync(Session.Character, new NosCore.Data.StaticEntities.CardDto { CardId = PermanentCardId },
                new List<NosCore.Data.StaticEntities.BCardDto>());

        private void ThePlayerStillHoldsThePermanentCard() => Assert.IsTrue(Buffs.HasBuff(Session.Character, PermanentCardId));

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
