//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.PacketHandlers.Battle;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.GameObject.Services.MovementService;
using NosCore.GameObject.Ecs.Extensions;
using Microsoft.Extensions.Logging;
using SpecLight;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Battle
{
    [TestClass]
    public class UseSkillPacketHandlerTests
    {
        private TestWorld World;
        private UseSkillPacketHandler Handler;
        private ClientSession Session;
        private ClientSession TargetSession;
        private Mock<IBattleService> BattleService;
        private readonly ILogger<UseSkillPacketHandler> Logger = new Mock<ILogger<UseSkillPacketHandler>>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            TargetSession = await World.CreateSessionAsync();
            BattleService = new Mock<IBattleService>();
            Gear.Setup(g => g.Resolve(It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>())).Returns(EquipmentStats.None);
            BattleService.Setup(x => x.Hit(It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>(),
                It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>(), It.IsAny<HitArguments>())).ReturnsAsync(true);

            Handler = new UseSkillPacketHandler(
                Logger,
                World.LogLanguageLocalizer,
                BattleService.Object,
                World.SessionRegistry,
                new SkillCastService(World.Clock, Gear.Object),
                World.DistanceCalculator,
                World.Create<WalkService>());
        }

        private readonly Mock<IEquipmentStatsService> Gear = new();

        [TestMethod]
        public async Task UsingSkillOnUnknownVisualTypeShouldBeIgnored()
        {
            await new Spec("Using skill on unknown visual type should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(UsingSkillOnUnknownVisualType)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingSkillOnNonExistentPlayerShouldBeIgnored()
        {
            await new Spec("Using skill on nonexistent player should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(UsingSkillOnNonExistentPlayer)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingSkillOnExistingPlayerShouldCallBattleService()
        {
            await new Spec("Using skill on existing player should call battle service")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(BattleServiceShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingSkillWhenCannotFightShouldSendCancelAndSkipBattleService()
        {
            await new Spec("CanFight=false short-circuits to a cancel packet and never calls Hit")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterCannotFight)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(ShouldReceiveCancelPacket)
                .And(BattleServiceShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingSkillWhileSittingStandsTheCharacterUpFirst()
        {
            await new Spec("A skill cast while sitting triggers RestAsync (IsSitting flips to false) before the Hit call")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterIsSitting)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(CharacterShouldNoLongerBeSitting)
                .And(BattleServiceShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingKnownSkillConsumesMpAndStampsLastUse()
        {
            await new Spec("A learned skill with MpCost>0 debits Mp and updates LastUse only after a successful Hit")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)15, (short)1)
                .And(CharacterHasMp_, 50)
                .And(HitTakes_Seconds, 2)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(BattleServiceShouldBeCalled)
                .And(CharacterMpShouldBe_, 35)
                .And(LastUseShouldBeWhenHitFinished)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AWornMpConsumptionDecreaseLowersTheCost()
        {
            await new Spec("A 20% MP consumption decrease on the gear makes a 15 MP skill cost 12")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)15, (short)1)
                .And(CharacterHasMp_, 50)
                .And(GearDecreasesMpConsumptionBy_, 20)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(BattleServiceShouldBeCalled)
                .And(CharacterMpShouldBe_, 38)
                .ExecuteAsync();
        }

        private void GearDecreasesMpConsumptionBy_(int percent) =>
            Gear.Setup(g => g.Resolve(It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>()))
                .Returns(EquipmentStats.None with { MpConsumptionDecrease = percent });

        [TestMethod]
        public async Task ARefusedCastSpendsNeitherMpNorCooldown()
        {
            await new Spec("A cast that BattleService refuses leaves Mp and LastUse untouched so the player can retry")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)15, (short)1)
                .And(CharacterHasMp_, 50)
                .And(BattleServiceRefusesTheCast)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(BattleServiceShouldBeCalled)
                .And(CharacterMpShouldBe_, 50)
                .And(LastUseShouldNotHaveBeenStamped)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingKnownSkillOnCooldownSendsCancelAndSkipsBattleService()
        {
            await new Spec("A skill whose cooldown has not elapsed since LastUse sends a cancel and does not call Hit")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)0, (short)300)
                .And(CharacterHasMp_, 50)
                .And(SkillWasUsedJustNow)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(ShouldReceiveCancelPacket)
                .And(BattleServiceShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingKnownSkillWithInsufficientMpSendsCancelAndSkipsBattleService()
        {
            await new Spec("A skill whose MpCost exceeds the character's Mp sends a cancel and does not call Hit")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)100, (short)1)
                .And(CharacterHasMp_, 20)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(ShouldReceiveCancelPacket)
                .And(BattleServiceShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ALearnedSkillOutOfReachSendsCancelAndSpendsNothing()
        {
            await new Spec("A target beyond the skill's range plus one cell gets a cancel, no Hit, and no mana spent")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)15, (short)1)
                .And(SkillHasRange_, (byte)1)
                .And(CharacterHasMp_, 50)
                .And(CharacterIsAt_WithSpeed_, (short)0, (byte)10)
                .And(TargetIsAt_, (short)5)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(ShouldReceiveCancelPacket)
                .And(BattleServiceShouldNotBeCalled)
                .And(CharacterMpShouldBe_, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AReportedCellOneStepAwayIsWhereTheCastIsMeasuredFrom()
        {
            await new Spec("A reported cell within one walk step is taken, which brings the target into reach")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)0, (short)1)
                .And(SkillHasRange_, (byte)1)
                .And(CharacterIsAt_WithSpeed_, (short)0, (byte)10)
                .And(TargetIsAt_, (short)6)
                .WhenAsync(UsingSkillOnExistingPlayerReportingCell_, (short)5)
                .Then(BattleServiceShouldBeCalled)
                .And(CharacterShouldBeAt_, (short)5)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AReportedCellBeyondOneStepIsIgnored()
        {
            await new Spec("A reported cell further than one walk step is ignored, so the cast stays out of reach")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)0, (short)1)
                .And(SkillHasRange_, (byte)1)
                .And(CharacterIsAt_WithSpeed_, (short)0, (byte)2)
                .And(TargetIsAt_, (short)5)
                .WhenAsync(UsingSkillOnExistingPlayerReportingCell_, (short)4)
                .Then(ShouldReceiveCancelPacket)
                .And(BattleServiceShouldNotBeCalled)
                .And(CharacterShouldBeAt_, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARefusedCastDoesNotKeepTheReportedCell()
        {
            await new Spec("A reported cell is dropped again when the cast is refused, so refused casts can't move the character")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)0, (short)1)
                .And(SkillHasRange_, (byte)1)
                .And(CharacterIsAt_WithSpeed_, (short)0, (byte)10)
                .And(TargetIsAt_, (short)20)
                .WhenAsync(UsingSkillOnExistingPlayerReportingCell_, (short)5)
                .Then(ShouldReceiveCancelPacket)
                .And(CharacterShouldBeAt_, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACastBattleServiceRefusesDoesNotKeepTheReportedCell()
        {
            await new Spec("A reported cell is dropped again when BattleService refuses the cast")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)0, (short)1)
                .And(SkillHasRange_, (byte)1)
                .And(CharacterIsAt_WithSpeed_, (short)0, (byte)10)
                .And(TargetIsAt_, (short)6)
                .And(BattleServiceRefusesTheCast)
                .WhenAsync(UsingSkillOnExistingPlayerReportingCell_, (short)5)
                .Then(BattleServiceShouldBeCalled)
                .And(CharacterShouldBeAt_, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReportedCellsAreNotTakenFasterThanTheCharacterCanWalk()
        {
            await new Spec("A second cast reporting a cell the character could not have walked to yet keeps the earlier cell")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)0, (short)1)
                .And(SkillHasRange_, (byte)10)
                .And(CharacterIsAt_WithSpeed_, (short)0, (byte)10)
                .And(TargetIsAt_, (short)6)
                .WhenAsync(UsingSkillOnExistingPlayerReportingCell_, (short)5)
                .AndAsync(UsingSkillOnExistingPlayerReportingCell_After_Milliseconds, (short)0, 200)
                .Then(CharacterShouldBeAt_, (short)5)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReportedCellsAtWalkingPaceAreTaken()
        {
            await new Spec("Casts while walking at the character's pace each take the reported cell")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)0, (short)1)
                .And(SkillHasRange_, (byte)10)
                .And(CharacterIsAt_WithSpeed_, (short)0, (byte)10)
                .And(TargetIsAt_, (short)6)
                .WhenAsync(UsingSkillOnExistingPlayerReportingCell_, (short)5)
                .AndAsync(UsingSkillOnExistingPlayerReportingCell_After_Milliseconds, (short)0, 1250)
                .Then(CharacterShouldBeAt_, (short)0)
                .ExecuteAsync();
        }

        private Task UsingSkillOnExistingPlayerReportingCell_After_Milliseconds(short x, int milliseconds)
        {
            World.Clock.AdvanceMilliseconds(milliseconds);
            return UsingSkillOnExistingPlayerReportingCell_(x);
        }

        [TestMethod]
        public async Task CastingInterruptsABarInProgress()
        {
            await new Spec("A skill cast during a delay bar cancels it")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .AndAsync(ADelayBarIsRunning)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(TheBarShouldNotCompleteAfterFiveSeconds)
                .ExecuteAsync();
        }

        private static readonly System.Guid BarTarget = System.Guid.NewGuid();

        private Task ADelayBarIsRunning()
        {
            return Session.Character.StartDelayAsync(new NosCore.Packets.ServerPackets.UI.DelayPacket
            {
                Delay = 5000,
                Type = NosCore.Packets.Enumerations.DelayPacketType.Locomotion,
            }, BarTarget, World.Clock.GetCurrentInstant());
        }

        private void TheBarShouldNotCompleteAfterFiveSeconds()
        {
            World.Clock.AdvanceSeconds(5);
            Assert.IsFalse(Session.Character.TryCompleteDelay(NosCore.Packets.Enumerations.DelayPacketType.Locomotion,
                BarTarget, World.Clock.GetCurrentInstant()));
        }

        [TestMethod]
        public async Task ASkillCarryingAnUpgradeLinkWithoutBeingAnUpgradeSpendsMpAndCooldown()
        {
            await new Spec("A learned skill with a non-zero UpgradeSkill that is not an upgrade, such as Capture, spends its mana and cooldown")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)15, (short)50)
                .And(SkillCarriesAnUpgradeLinkWithoutBeingAnUpgrade)
                .And(CharacterHasMp_, 50)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(BattleServiceShouldBeCalled)
                .And(CharacterMpShouldBe_, 35)
                .And(LastUseShouldBeWhenHitFinished)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillCarryingAnUpgradeLinkWithoutBeingAnUpgradeHonoursItsCooldown()
        {
            await new Spec("A learned skill with a non-zero UpgradeSkill that is not an upgrade is refused while on cooldown")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)0, (short)300)
                .And(SkillCarriesAnUpgradeLinkWithoutBeingAnUpgrade)
                .And(SkillWasUsedJustNow)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(ShouldReceiveCancelPacket)
                .And(BattleServiceShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACasterCentredSkillNamingATargetOutsideItsAreaIsRefused()
        {
            await new Spec("A skill centred on the caster is refused when the target it names stands outside its area")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)15, (short)1)
                .And(SkillIsCentredOnTheCasterWithArea_, (byte)2)
                .And(CharacterHasMp_, 50)
                .And(CharacterIsAt_WithSpeed_, (short)0, (byte)10)
                .And(TargetIsAt_, (short)10)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(ShouldReceiveCancelPacket)
                .And(BattleServiceShouldNotBeCalled)
                .And(CharacterMpShouldBe_, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACasterCentredSkillNamingATargetInsideItsAreaGoesThrough()
        {
            await new Spec("A skill centred on the caster goes through when the target it names stands inside its area")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)15, (short)1)
                .And(SkillIsCentredOnTheCasterWithArea_, (byte)2)
                .And(CharacterHasMp_, 50)
                .And(CharacterIsAt_WithSpeed_, (short)0, (byte)10)
                .And(TargetIsAt_, (short)2)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(BattleServiceShouldBeCalled)
                .And(CharacterMpShouldBe_, 35)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATargetedSkillWithoutARangeOnlyReachesMeleeDistance()
        {
            await new Spec("A targeted skill whose data has no range is refused on a target beyond melee reach")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)0, (short)1)
                .And(SkillHasRange_, (byte)0)
                .And(CharacterIsAt_WithSpeed_, (short)0, (byte)10)
                .And(TargetIsAt_, (short)20)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(ShouldReceiveCancelPacket)
                .And(BattleServiceShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANormalRotationOfSkillsGoesThrough()
        {
            await new Spec("Two skills cast back to back both go through, and the first is usable again once its cooldown is over")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .And(CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_, (short)1, (short)10, (short)10)
                .And(CharacterAlsoKnowsSkillWithCastId_, (short)2)
                .And(CharacterHasMp_, 100)
                .And(CharacterIsAt_WithSpeed_, (short)0, (byte)10)
                .And(TargetIsAt_, (short)1)
                .WhenAsync(UsingSkill_OnExistingPlayer, (short)1)
                .AndAsync(UsingSkill_OnExistingPlayer, (short)2)
                .And(TimePasses_Seconds, 2)
                .AndAsync(UsingSkill_OnExistingPlayer, (short)1)
                .Then(BattleServiceShouldBeCalled_Times, 3)
                .And(CharacterMpShouldBe_, 70)
                .ExecuteAsync();
        }

        private void SkillCarriesAnUpgradeLinkWithoutBeingAnUpgrade()
        {
            _learnedSkill.Skill.UpgradeSkill = 200;
            _learnedSkill.Skill.SkillType = 1;
        }

        private void SkillIsCentredOnTheCasterWithArea_(byte area)
        {
            _learnedSkill.Skill.TargetType = 1;
            _learnedSkill.Skill.Range = 0;
            _learnedSkill.Skill.TargetRange = area;
        }

        private void CharacterAlsoKnowsSkillWithCastId_(short castId)
        {
            Session.Character.Skills[2] = new CharacterSkill
            {
                Skill = new SkillDto { SkillVNum = 2, CastId = castId, MpCost = 10, Cooldown = 10, Range = 2 },
            };
        }

        private void TimePasses_Seconds(int seconds) => World.Clock.AdvanceSeconds(seconds);

        private async Task UsingSkill_OnExistingPlayer(short castId)
        {
            await Handler.ExecuteAsync(new UseSkillPacket
            {
                CastId = castId,
                TargetVisualType = VisualType.Player,
                TargetId = TargetSession.Character.VisualId
            }, Session);
        }

        private void BattleServiceShouldBeCalled_Times(int times)
        {
            BattleService.Verify(x => x.Hit(It.IsAny<NosCore.GameObject.Ecs.Interfaces.ICharacterEntity>(),
                It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>(),
                It.IsAny<HitArguments>()), Times.Exactly(times));
        }

        private void SkillHasRange_(byte range)
        {
            _learnedSkill.Skill.Range = range;
            _learnedSkill.Skill.TargetType = 0;
        }

        private void CharacterIsAt_WithSpeed_(short x, byte speed)
        {
            Session.Character.PositionX = x;
            Session.Character.PositionY = 0;
            Session.Character.Speed = speed;
        }

        private void TargetIsAt_(short x)
        {
            TargetSession.Character.PositionX = x;
            TargetSession.Character.PositionY = 0;
        }

        private void CharacterShouldBeAt_(short x)
        {
            Assert.AreEqual(x, Session.Character.PositionX);
        }

        private async Task UsingSkillOnExistingPlayerReportingCell_(short x)
        {
            await Handler.ExecuteAsync(new UseSkillPacket
            {
                CastId = 1,
                TargetVisualType = VisualType.Player,
                TargetId = TargetSession.Character.VisualId,
                MapX = x,
                MapY = 0
            }, Session);
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private void TargetIsOnSameMap()
        {
            TargetSession.Character.MapInstance = World.GetBaseMap(1);
        }

        private void CharacterCannotFight()
        {
            Session.Character.CanFight = false;
        }

        private void CharacterIsSitting()
        {
            Session.Character.IsSitting = true;
        }

        private CharacterSkill _learnedSkill;

        private void CharacterHasLearnedSkillWithCastId_MpCost_Cooldown_(short castId, short mpCost, short cooldown)
        {
            _learnedSkill = new CharacterSkill
            {
                Skill = new SkillDto
                {
                    SkillVNum = 1,
                    CastId = castId,
                    UpgradeSkill = 0,
                    MpCost = mpCost,
                    Cooldown = cooldown,
                },
            };
            Session.Character.Skills = new ConcurrentDictionary<short, CharacterSkill>();
            Session.Character.Skills[1] = _learnedSkill;
        }

        private void CharacterHasMp_(int mp)
        {
            Session.Character.Mp = mp;
        }

        private void HitTakes_Seconds(int seconds)
        {
            BattleService.Setup(s => s.Hit(It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>(), It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>(), It.IsAny<HitArguments>()))
                .Callback(() => World.Clock.AdvanceSeconds(seconds))
                .ReturnsAsync(true);
        }

        private void SkillWasUsedJustNow()
        {
            _learnedSkill.LastUse = World.Clock.GetCurrentInstant();
        }

        private async Task UsingSkillOnUnknownVisualType()
        {
            await Handler.ExecuteAsync(new UseSkillPacket
            {
                CastId = 1,
                TargetVisualType = (VisualType)99,
                TargetId = 1
            }, Session);
        }

        private async Task UsingSkillOnNonExistentPlayer()
        {
            await Handler.ExecuteAsync(new UseSkillPacket
            {
                CastId = 1,
                TargetVisualType = VisualType.Player,
                TargetId = 99999
            }, Session);
        }

        private async Task UsingSkillOnExistingPlayer()
        {
            await Handler.ExecuteAsync(new UseSkillPacket
            {
                CastId = 1,
                TargetVisualType = VisualType.Player,
                TargetId = TargetSession.Character.VisualId
            }, Session);
        }

        private void ShouldReceiveCancelPacket()
        {
            var packet = Session.LastPackets.OfType<CancelPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void BattleServiceShouldBeCalled()
        {
            BattleService.Verify(x => x.Hit(It.IsAny<NosCore.GameObject.Ecs.Interfaces.ICharacterEntity>(),
                It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>(),
                It.IsAny<HitArguments>()), Times.Once);
        }

        private void BattleServiceShouldNotBeCalled()
        {
            BattleService.Verify(x => x.Hit(It.IsAny<NosCore.GameObject.Ecs.Interfaces.ICharacterEntity>(),
                It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>(),
                It.IsAny<HitArguments>()), Times.Never);
        }

        private void BattleServiceRefusesTheCast()
        {
            BattleService.Setup(x => x.Hit(It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>(),
                It.IsAny<NosCore.GameObject.Ecs.Interfaces.IAliveEntity>(), It.IsAny<HitArguments>())).ReturnsAsync(false);
        }

        private void CharacterShouldNoLongerBeSitting()
        {
            Assert.IsFalse(Session.Character.IsSitting);
        }

        private void CharacterMpShouldBe_(int expected)
        {
            Assert.AreEqual(expected, Session.Character.Mp);
        }

        private void LastUseShouldBeWhenHitFinished()
        {
            Assert.AreEqual(World.Clock.GetCurrentInstant(), _learnedSkill.LastUse);
        }

        private void LastUseShouldNotHaveBeenStamped()
        {
            Assert.IsNull(_learnedSkill.LastUse);
        }
    }
}
