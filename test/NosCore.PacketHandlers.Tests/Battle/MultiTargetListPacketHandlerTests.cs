//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MovementService;
using NosCore.PacketHandlers.Battle;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Battle
{
    [TestClass]
    public class MultiTargetListPacketHandlerTests
    {
        private TestWorld World;
        private MultiTargetListPacketHandler _listHandler;
        private UseSkillPacketHandler _castHandler;
        private ClientSession _session;
        private MapInstance _map;
        private Mock<IBattleService> _battleService;
        private readonly Mock<IEquipmentStatsService> _gear = new();
        private List<HitArguments> _casts = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _map = World.GetBaseMap(1);
            _session.Character.MapInstance = _map;
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _gear.Setup(g => g.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            _battleService = new Mock<IBattleService>();
            _battleService.Setup(b => b.Hit(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<HitArguments>()))
                .Callback<IAliveEntity, IAliveEntity, HitArguments>((_, _, arguments) => _casts.Add(arguments))
                .ReturnsAsync(true);
            _listHandler = new MultiTargetListPacketHandler();
            _castHandler = new UseSkillPacketHandler(
                new Mock<ILogger<UseSkillPacketHandler>>().Object,
                World.LogLanguageLocalizer,
                _battleService.Object,
                World.SessionRegistry,
                new SkillCastService(World.Clock, _gear.Object, World.Create<BuffService>()),
                World.DistanceCalculator,
                new WalkService(World.Clock));
        }

        [TestMethod]
        public async Task TheNextCastHitsTheListedMonstersInRange()
        {
            await new Spec("The cast after an mtlist carries the listed monsters within the skill's range")
                .Given(MonstersStandAtX_, new short[] { 2, 3, 4, 20 })
                .And(TheCharacterKnowsASkillOfRange_, (byte)5)
                .WhenAsync(Listing_, new long[] { 201, 202, 203, 999 })
                .AndAsync(CastingOnMonster_, 200L)
                .Then(TheListedTargetsShouldBe_, new long[] { 201, 202 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheListOnlyServesOneCast()
        {
            await new Spec("A second cast without a new mtlist hits only its own target")
                .Given(MonstersStandAtX_, new short[] { 2, 3 })
                .And(TheCharacterKnowsASkillOfRange_, (byte)5)
                .WhenAsync(Listing_, new long[] { 201 })
                .AndAsync(CastingOnMonster_, 200L)
                .AndAsync(CastingOnMonster_, 200L)
                .Then(TheLastCastShouldListNothing)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AListWhoseCountDoesNotMatchIsIgnored()
        {
            await new Spec("A list whose announced count differs from its entries is dropped")
                .Given(MonstersStandAtX_, new short[] { 2, 3 })
                .And(TheCharacterKnowsASkillOfRange_, (byte)5)
                .WhenAsync(ListingWithCount_, (byte)3)
                .AndAsync(CastingOnMonster_, 200L)
                .Then(TheLastCastShouldListNothing)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnlearnedSkillTakesNoListedTargets()
        {
            await new Spec("A cast id the character has not learned carries no listed target")
                .Given(MonstersStandAtX_, new short[] { 2, 3 })
                .And(TheCharacterKnowsNoSkill)
                .WhenAsync(Listing_, new long[] { 201 })
                .AndAsync(CastingOnMonster_, 200L)
                .Then(TheLastCastShouldListNothing)
                .ExecuteAsync();
        }

        private void MonstersStandAtX_(short[] xs)
        {
            var template = new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 100, MaxMp = 50, Level = 5 };
            _map.LoadMonsters(
                xs.Select((x, i) => new MapMonsterDto { MapMonsterId = 200 + i, MapId = 1, MapX = x, MapY = 0, VNum = 1 }).ToList(),
                new List<NpcMonsterDto> { template });
        }

        private void TheCharacterKnowsASkillOfRange_(byte range)
        {
            _session.Character.Skills = new ConcurrentDictionary<short, CharacterSkill>
            {
                [1] = new() { Skill = new SkillDto { SkillVNum = 1, CastId = 5, Range = range, TargetRange = 3, HitType = 3, SkillType = 1 } },
            };
            _session.Character.Mp = 100;
        }

        private void TheCharacterKnowsNoSkill() => _session.Character.Skills = new ConcurrentDictionary<short, CharacterSkill>();

        private Task Listing_(long[] ids) => _listHandler.ExecuteAsync(new MultiTargetListPacket
        {
            TargetsAmount = (byte)ids.Length,
            Targets = ids.Select(id => new MultiTargetListSubPacket { VisualType = VisualType.Monster, VisualId = id }).ToList(),
        }, _session);

        private Task ListingWithCount_(byte count) => _listHandler.ExecuteAsync(new MultiTargetListPacket
        {
            TargetsAmount = count,
            Targets = new List<MultiTargetListSubPacket>
            {
                new() { VisualType = VisualType.Monster, VisualId = 201 },
            },
        }, _session);

        private async Task CastingOnMonster_(long id)
        {
            foreach (var skill in _session.Character.Skills.Values)
            {
                skill.LastUse = null;
            }

            await _castHandler.ExecuteAsync(new UseSkillPacket
            {
                CastId = 5,
                TargetVisualType = VisualType.Monster,
                TargetId = id,
            }, _session);
        }

        private void TheListedTargetsShouldBe_(long[] ids) =>
            CollectionAssert.AreEqual(ids, _casts.Last().ListedTargets.NotNull().Select(t => t.VisualId).ToArray());

        private void TheLastCastShouldListNothing() =>
            Assert.IsTrue(_casts.Last().ListedTargets is null or { Count: 0 });
    }
}
