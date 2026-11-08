//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.PacketHandlers.Battle;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Battle
{
    [TestClass]
    public class UseAoeSkillPacketHandlerTests
    {
        private UseAoeSkillPacketHandler _handler = null!;
        private ClientSession _session = null!;
        private Mock<IBattleService> _battleService = null!;
        private readonly Mock<IEquipmentStatsService> _gear = new();
        private CharacterSkill _skill = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _gear.Setup(g => g.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            _battleService = new Mock<IBattleService>();
            _battleService.Setup(b => b.HitArea(It.IsAny<IAliveEntity>(), It.IsAny<short>(), It.IsAny<short>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(true);
            _handler = new UseAoeSkillPacketHandler(
                _battleService.Object,
                new SkillCastService(TestHelpers.Instance.Clock, _gear.Object),
                TestHelpers.Instance.DistanceCalculator);
        }

        [TestMethod]
        public async Task ACastWithinRangeHitsTheCellAndCostsManaAndCooldown()
        {
            await new Spec("A ground skill cast within its range goes to the battle service at that cell and spends its mana and cooldown")
                .Given(TheCharacterKnowsAGroundSkillOfRange_MpCost_Cooldown_, (byte)5, (short)20, (short)50)
                .And(TheCharacterHas_Mp, 50)
                .WhenAsync(CastingAt_, (short)4)
                .Then(TheAreaShouldBeHitAt_, (short)4)
                .And(TheCharacterShouldHave_Mp, 30)
                .And(TheSkillShouldBeOnCooldown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACellBeyondTheSkillsRangeIsRefused()
        {
            await new Spec("A cell further than the skill's range plus one cell is cancelled and costs nothing")
                .Given(TheCharacterKnowsAGroundSkillOfRange_MpCost_Cooldown_, (byte)5, (short)20, (short)50)
                .And(TheCharacterHas_Mp, 50)
                .WhenAsync(CastingAt_, (short)7)
                .Then(ACancelShouldBeSent)
                .And(TheAreaShouldNotBeHit)
                .And(TheCharacterShouldHave_Mp, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillOnCooldownIsRefused()
        {
            await new Spec("A ground skill still on cooldown is cancelled")
                .Given(TheCharacterKnowsAGroundSkillOfRange_MpCost_Cooldown_, (byte)5, (short)20, (short)50)
                .And(TheCharacterHas_Mp, 50)
                .And(TheSkillWasJustUsed)
                .WhenAsync(CastingAt_, (short)4)
                .Then(ACancelShouldBeSent)
                .And(TheAreaShouldNotBeHit)
                .And(TheCharacterShouldHave_Mp, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NotEnoughManaIsRefusedWithTheClientsMessage()
        {
            await new Spec("A ground skill the character cannot pay for is cancelled with the not-enough-MP message")
                .Given(TheCharacterKnowsAGroundSkillOfRange_MpCost_Cooldown_, (byte)5, (short)80, (short)50)
                .And(TheCharacterHas_Mp, 50)
                .WhenAsync(CastingAt_, (short)4)
                .Then(ACancelShouldBeSent)
                .And(TheNotEnoughMpMessageShouldBeSent)
                .And(TheAreaShouldNotBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnknownSkillIsRefused()
        {
            await new Spec("A cast id the character has not learned is cancelled")
                .Given(TheCharacterKnowsNoSkill)
                .WhenAsync(CastingAt_, (short)4)
                .Then(ACancelShouldBeSent)
                .And(TheAreaShouldNotBeHit)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACastTheBattleServiceRefusesCostsNothing()
        {
            await new Spec("A cast the battle service refuses spends neither mana nor cooldown")
                .Given(TheCharacterKnowsAGroundSkillOfRange_MpCost_Cooldown_, (byte)5, (short)20, (short)50)
                .And(TheCharacterHas_Mp, 50)
                .And(TheBattleServiceRefuses)
                .WhenAsync(CastingAt_, (short)4)
                .Then(TheCharacterShouldHave_Mp, 50)
                .And(TheSkillShouldNotBeOnCooldown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AVehicledCharacterCannotCast()
        {
            await new Spec("A character on a vehicle is cancelled")
                .Given(TheCharacterKnowsAGroundSkillOfRange_MpCost_Cooldown_, (byte)5, (short)20, (short)50)
                .And(TheCharacterIsVehicled)
                .WhenAsync(CastingAt_, (short)4)
                .Then(ACancelShouldBeSent)
                .And(TheAreaShouldNotBeHit)
                .ExecuteAsync();
        }

        private void TheCharacterKnowsAGroundSkillOfRange_MpCost_Cooldown_(byte range, short mpCost, short cooldown)
        {
            _skill = new CharacterSkill
            {
                Skill = new SkillDto { SkillVNum = 1, CastId = 3, Range = range, MpCost = mpCost, Cooldown = cooldown, HitType = 4 },
            };
            _session.Character.Skills = new ConcurrentDictionary<short, CharacterSkill> { [1] = _skill };
        }

        private void TheCharacterKnowsNoSkill()
        {
            _session.Character.Skills = new ConcurrentDictionary<short, CharacterSkill>();
        }

        private void TheCharacterHas_Mp(int mp) => _session.Character.Mp = mp;

        private void TheSkillWasJustUsed() => _skill.LastUse = TestHelpers.Instance.Clock.GetCurrentInstant();

        private void TheCharacterIsVehicled() => _session.Character.IsVehicled = true;

        private void TheBattleServiceRefuses() =>
            _battleService.Setup(b => b.HitArea(It.IsAny<IAliveEntity>(), It.IsAny<short>(), It.IsAny<short>(), It.IsAny<HitArguments>()))
                .ReturnsAsync(false);

        private Task CastingAt_(short x) =>
            _handler.ExecuteAsync(new UseAoeSkillPacket { CastId = 3, MapX = x, MapY = 0 }, _session);

        private void TheAreaShouldBeHitAt_(short x)
        {
            var character = _session.Character;
            _battleService.Verify(b => b.HitArea(character, x, 0, It.Is<HitArguments>(a => a.SkillId == 3)), Times.Once);
        }

        private void TheAreaShouldNotBeHit() =>
            _battleService.Verify(b => b.HitArea(It.IsAny<IAliveEntity>(), It.IsAny<short>(), It.IsAny<short>(), It.IsAny<HitArguments>()), Times.Never);

        private void ACancelShouldBeSent() => Assert.IsNotNull(_session.LastPackets.OfType<CancelPacket>().FirstOrDefault());

        private void TheNotEnoughMpMessageShouldBeSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.NotEnoughMpForSpell));

        private void TheCharacterShouldHave_Mp(int mp) => Assert.AreEqual(mp, _session.Character.Mp);

        private void TheSkillShouldBeOnCooldown() => Assert.AreEqual(TestHelpers.Instance.Clock.GetCurrentInstant(), _skill.LastUse);

        private void TheSkillShouldNotBeOnCooldown() => Assert.IsNull(_skill.LastUse);
    }
}
