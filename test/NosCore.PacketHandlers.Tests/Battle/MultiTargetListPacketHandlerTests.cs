//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.PacketHandlers.Battle;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Battle
{
    [TestClass]
    public class MultiTargetListPacketHandlerTests
    {
        private MultiTargetListPacketHandler _handler = null!;
        private ClientSession _session = null!;
        private MapInstance _map = null!;
        private Mock<IBattleService> _battleService = null!;
        private readonly Mock<IEquipmentStatsService> _gear = new();
        private CharacterSkill _skill = null!;
        private List<long> _targetsHit = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _map = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _session.Character.MapInstance = _map;
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _gear.Setup(g => g.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            _battleService = new Mock<IBattleService>();
            _battleService.Setup(b => b.HitTargets(It.IsAny<IAliveEntity>(), It.IsAny<IReadOnlyList<IAliveEntity>>(), It.IsAny<HitArguments>()))
                .Callback<IAliveEntity, IReadOnlyList<IAliveEntity>, HitArguments>((_, targets, _) => _targetsHit = targets.Select(t => t.VisualId).ToList())
                .ReturnsAsync(true);
            _handler = new MultiTargetListPacketHandler(
                _battleService.Object,
                new SkillCastService(TestHelpers.Instance.Clock, _gear.Object),
                TestHelpers.Instance.DistanceCalculator);
        }

        [TestMethod]
        public async Task EachListedMonsterInRangeIsHit()
        {
            await new Spec("Every listed monster within the skill's range goes to the battle service in one cast")
                .Given(MonstersStandAtX_, new short[] { 2, 3, 4 })
                .And(TheCharacterKnowsASkillOfRange_MpCost_, (byte)5, (short)20)
                .And(TheCharacterHas_Mp, 50)
                .WhenAsync(Listing_, new[] { 200, 201, 202 })
                .Then(TheTargetsHitShouldBe_, new long[] { 200, 201, 202 })
                .And(TheCharacterShouldHave_Mp, 30)
                .And(TheSkillShouldBeOnCooldown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OutOfRangeAndUnknownTargetsAreIgnored()
        {
            await new Spec("A monster beyond the skill's range and an id that is not on the map are dropped from the list")
                .Given(MonstersStandAtX_, new short[] { 2, 20 })
                .And(TheCharacterKnowsASkillOfRange_MpCost_, (byte)5, (short)20)
                .And(TheCharacterHas_Mp, 50)
                .WhenAsync(Listing_, new[] { 200, 201, 999 })
                .Then(TheTargetsHitShouldBe_, new long[] { 200 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AListWithNoTargetInRangeCostsNothing()
        {
            await new Spec("When no listed monster is in range nothing is cast and nothing is spent")
                .Given(MonstersStandAtX_, new short[] { 20 })
                .And(TheCharacterKnowsASkillOfRange_MpCost_, (byte)5, (short)20)
                .And(TheCharacterHas_Mp, 50)
                .WhenAsync(Listing_, new[] { 200 })
                .Then(TheBattleServiceShouldNotBeCalled)
                .And(TheCharacterShouldHave_Mp, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillOnCooldownIsNotCast()
        {
            await new Spec("A skill still on cooldown hits nothing and spends nothing")
                .Given(MonstersStandAtX_, new short[] { 2 })
                .And(TheCharacterKnowsASkillOfRange_MpCost_, (byte)5, (short)20)
                .And(TheCharacterHas_Mp, 50)
                .And(TheSkillWasJustUsed)
                .WhenAsync(Listing_, new[] { 200 })
                .Then(TheBattleServiceShouldNotBeCalled)
                .And(TheCharacterShouldHave_Mp, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NotEnoughManaIsNotCast()
        {
            await new Spec("A skill the character cannot pay for hits nothing and shows the not-enough-MP message")
                .Given(MonstersStandAtX_, new short[] { 2 })
                .And(TheCharacterKnowsASkillOfRange_MpCost_, (byte)5, (short)80)
                .And(TheCharacterHas_Mp, 50)
                .WhenAsync(Listing_, new[] { 200 })
                .Then(TheBattleServiceShouldNotBeCalled)
                .And(TheNotEnoughMpMessageShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AListWhoseCountDoesNotMatchIsIgnored()
        {
            await new Spec("A list whose announced count differs from its entries is ignored")
                .Given(MonstersStandAtX_, new short[] { 2, 3 })
                .And(TheCharacterKnowsASkillOfRange_MpCost_, (byte)5, (short)20)
                .And(TheCharacterHas_Mp, 50)
                .WhenAsync(ListingWithCount_, (byte)3)
                .Then(TheBattleServiceShouldNotBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnlearnedSkillIsNotCast()
        {
            await new Spec("A cast id the character has not learned hits nothing")
                .Given(MonstersStandAtX_, new short[] { 2 })
                .And(TheCharacterKnowsNoSkill)
                .WhenAsync(Listing_, new[] { 200 })
                .Then(TheBattleServiceShouldNotBeCalled)
                .ExecuteAsync();
        }

        private void MonstersStandAtX_(short[] xs)
        {
            var template = new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 100, MaxMp = 50, Level = 5 };
            _map.LoadMonsters(
                xs.Select((x, i) => new MapMonsterDto { MapMonsterId = 200 + i, MapId = 1, MapX = x, MapY = 0, VNum = 1 }).ToList(),
                new List<NpcMonsterDto> { template });
        }

        private void TheCharacterKnowsASkillOfRange_MpCost_(byte range, short mpCost)
        {
            _skill = new CharacterSkill
            {
                Skill = new SkillDto { SkillVNum = 1, CastId = 3, Range = range, MpCost = mpCost, Cooldown = 50, HitType = 5 },
            };
            _session.Character.Skills = new ConcurrentDictionary<short, CharacterSkill> { [1] = _skill };
        }

        private void TheCharacterKnowsNoSkill() => _session.Character.Skills = new ConcurrentDictionary<short, CharacterSkill>();

        private void TheCharacterHas_Mp(int mp) => _session.Character.Mp = mp;

        private void TheSkillWasJustUsed() => _skill.LastUse = TestHelpers.Instance.Clock.GetCurrentInstant();

        private Task Listing_(int[] ids) => _handler.ExecuteAsync(new MultiTargetListPacket
        {
            TargetsAmount = (byte)ids.Length,
            Targets = ids.Select(id => new MultiTargetListSubPacket { SkillCastId = 3, TargetId = id }).ToList(),
        }, _session);

        private Task ListingWithCount_(byte count) => _handler.ExecuteAsync(new MultiTargetListPacket
        {
            TargetsAmount = count,
            Targets = new List<MultiTargetListSubPacket>
            {
                new() { SkillCastId = 3, TargetId = 200 },
                new() { SkillCastId = 3, TargetId = 201 },
            },
        }, _session);

        private void TheTargetsHitShouldBe_(long[] ids) => CollectionAssert.AreEqual(ids, _targetsHit);

        private void TheBattleServiceShouldNotBeCalled() =>
            _battleService.Verify(b => b.HitTargets(It.IsAny<IAliveEntity>(), It.IsAny<IReadOnlyList<IAliveEntity>>(), It.IsAny<HitArguments>()), Times.Never);

        private void TheNotEnoughMpMessageShouldBeSent() =>
            Assert.IsTrue(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.NotEnoughMpForSpell));

        private void TheCharacterShouldHave_Mp(int mp) => Assert.AreEqual(mp, _session.Character.Mp);

        private void TheSkillShouldBeOnCooldown() => Assert.AreEqual(TestHelpers.Instance.Clock.GetCurrentInstant(), _skill.LastUse);
    }
}
