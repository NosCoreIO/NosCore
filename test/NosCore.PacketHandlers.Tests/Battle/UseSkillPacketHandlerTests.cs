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
using NosCore.PacketHandlers.Battle;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
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
        private UseSkillPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private ClientSession TargetSession = null!;
        private Mock<IBattleService> BattleService = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            BattleService = new Mock<IBattleService>();

            Handler = new UseSkillPacketHandler(
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                BattleService.Object,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task UsingSkillWhenVehicledShouldSendCancelPacket()
        {
            await new Spec("Using skill when vehicled should send cancel packet")
                .Given(CharacterIsOnMap)
                .And(CharacterIsVehicled)
                .WhenAsync(UsingSkill)
                .Then(ShouldReceiveCancelPacket)
                .ExecuteAsync();
        }

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
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(BattleServiceShouldBeCalled)
                .And(CharacterMpShouldBe_, 35)
                .And(LastUseShouldHaveBeenStampedRecent)
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

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void TargetIsOnSameMap()
        {
            TargetSession.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void CharacterIsVehicled()
        {
            Session.Character.IsVehicled = true;
        }

        private void CharacterCannotFight()
        {
            Session.Character.CanFight = false;
        }

        private void CharacterIsSitting()
        {
            Session.Character.IsSitting = true;
        }

        private CharacterSkill _learnedSkill = null!;

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

        private void SkillWasUsedJustNow()
        {
            _learnedSkill.LastUse = DateTime.Now;
        }

        private async Task UsingSkill()
        {
            await Handler.ExecuteAsync(new UseSkillPacket
            {
                CastId = 1,
                TargetVisualType = VisualType.Player,
                TargetId = Session.Character.VisualId
            }, Session);
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

        private void CharacterShouldNoLongerBeSitting()
        {
            Assert.IsFalse(Session.Character.IsSitting);
        }

        private void CharacterMpShouldBe_(int expected)
        {
            Assert.AreEqual(expected, Session.Character.Mp);
        }

        private void LastUseShouldHaveBeenStampedRecent()
        {
            Assert.IsTrue(DateTime.Now - _learnedSkill.LastUse < TimeSpan.FromSeconds(5));
        }
    }
}
