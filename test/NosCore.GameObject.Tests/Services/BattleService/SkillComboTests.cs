//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using SpecLight;
using Wolverine;
using FakeEntity = NosCore.GameObject.Tests.Services.BattleService.BattleServiceTests.FakeEntity;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SkillComboTests
    {
        private const short BasicAttack = 811;
        private const short OtherSkill = 812;

        private static readonly List<ComboDto> Combos = new()
        {
            new ComboDto { ComboId = 1, SkillVNum = BasicAttack, Hit = 2, Animation = 12, Effect = 4101 },
            new ComboDto { ComboId = 2, SkillVNum = BasicAttack, Hit = 3, Animation = 13, Effect = 4102 },
        };

        private TestWorld World;
        private ClientSession _caster;
        private MapInstance _map;
        private Mock<ISkillResolver> _skillResolver;
        private GameObject.Services.BattleService.BattleService _service;
        private readonly List<short> _drawnEffects = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _caster = await World.CreateSessionAsync();
            _map = World.GetBaseMap(0);
            _caster.Character.MapInstance = _map;

            _skillResolver = new Mock<ISkillResolver>();
            IAliveEntity caster = _caster.Character;
            _skillResolver.Setup(r => r.Resolve(caster, 0L)).Returns(Skill(BasicAttack, 0));
            _skillResolver.Setup(r => r.Resolve(caster, 1L)).Returns(Skill(OtherSkill, 1));

            var targetResolver = new Mock<ITargetResolver>();
            targetResolver.Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(), It.IsAny<SkillInfo>()))
                .Returns<IAliveEntity, IAliveEntity, SkillInfo>((_, target, _) => new List<IAliveEntity> { target });
            var hitQueue = new Mock<IHitQueue>();
            hitQueue.Setup(q => q.EnqueueAsync(It.IsAny<HitRequest>()))
                .Callback<HitRequest>(r => _drawnEffects.Add(r.Skill.Effect))
                .ReturnsAsync(new HitOutcome(HitStatus.Landed, 10, SuPacketHitMode.SuccessAttack, false));
            var buffService = new Mock<IBuffService>();
            buffService.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(Array.Empty<BuffInstance>());

            _service = new GameObject.Services.BattleService.BattleService(
                _skillResolver.Object,
                targetResolver.Object,
                hitQueue.Object,
                new Mock<IMessageBus>().Object,
                World.SessionRegistry,
                NodaTime.SystemClock.Instance,
                new Mock<ICaptureService>().Object,
                new Mock<ICastDelayService>().Object,
                buffService.Object,
                new SkillCastService(NodaTime.SystemClock.Instance, new Mock<GameObject.Services.EquipmentService.IEquipmentStatsService> { DefaultValue = DefaultValue.Empty }.Object, buffService.Object),
                new SkillComboService(Combos),
                NullLogger<GameObject.Services.BattleService.BattleService>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task ABasicAttackStreakPlaysItsComboAnimations()
        {
            await new Spec("The second and third blows in a row are drawn with the combo rows, then the streak starts over")
                .WhenAsync(TheCasterUses_, new long[] { 0, 0, 0, 0 })
                .Then(TheEffectsShouldBe_, new short[] { 4100, 4101, 4102, 4100 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherSkillBreaksTheStreak()
        {
            await new Spec("Using another skill in between restarts the combo from its first blow")
                .WhenAsync(TheCasterUses_, new long[] { 0, 1, 0, 0 })
                .Then(TheEffectsShouldBe_, new short[] { 4100, 9000, 4100, 4101 })
                .ExecuteAsync();
        }

        private static SkillInfo Skill(short vnum, long castId) => new(
            SkillVnum: vnum, CastId: castId, Cooldown: 0, AttackAnimation: 11,
            CastEffect: 0, Effect: vnum == BasicAttack ? (short)4100 : (short)9000,
            Type: 0, HitType: TargetHitType.SingleTargetHit, Range: 2, TargetRange: 0, TargetType: 0,
            Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        private async Task TheCasterUses_(long[] castIds)
        {
            foreach (var castId in castIds)
            {
                var monster = new FakeEntity { Hp = 100, VisualId = 50, VisualType = VisualType.Monster, MapInstance = _map };
                Assert.IsTrue(await _service.Hit(_caster.Character, monster, new HitArguments { SkillId = castId }));
            }
        }

        private void TheEffectsShouldBe_(short[] effects) => CollectionAssert.AreEqual(effects, _drawnEffects);
    }
}
