//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.MateExperienceService;
using NosCore.GameObject.Services.MateService;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceShareService;
using NosCore.Algorithm.LevelPenaltyService;
using NosCore.Algorithm.LowLevelBoostService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class RewardExperienceTests
    {
        private ClientSession _session = null!;
        private MonsterComponentBundle _monster;
        private RewardService _service = null!;
        private readonly Dictionary<long, (long Xp, long JobXp)> _awarded = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            var map = _session.Character.MapInstance;
            var creature = new NpcMonsterDto
            {
                NpcMonsterVNum = 333, Level = 30, MaxHp = 100, Xp = 1000, JobXp = 100, Name = new I18NString(),
            };
            _monster = new MonsterComponentBundle(
                map.EcsWorld.CreateMonster(9001, creature, map, 12, 14, 2, 12, 14, false, true, false), map.EcsWorld);

            var progression = new Mock<IExperienceProgressionService>();
            progression.Setup(p => p.AddExperienceAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<long>(),
                    It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()))
                .Callback((PlayerComponentBundle player, long xp, long jobXp, long _, long _, int _) =>
                    _awarded[player.CharacterId] = (xp, jobXp))
                .Returns(Task.CompletedTask);
            var catalog = new Mock<INpcCombatCatalog>();
            catalog.Setup(c => c.GetDrops(It.IsAny<short>())).Returns(new List<DropDto>());
            _service = new RewardService(new Mock<IItemGenerationService>().Object,
                new Mock<IMapItemGenerationService>().Object, catalog.Object,
                progression.Object, new ExperienceShareService(), new LevelPenaltyService(),
                new LowLevelBoostService(), TestHelpers.Instance.GroupLootService, new MateProgressionService(new MateExperienceService()), NullLogger<RewardService>.Instance);
        }

        [TestMethod]
        public async Task ASoloKillGivesTheWholeExperience()
        {
            _session.Character.Level = 30;
            _session.Character.JobLevel = 30;

            await KillAsync((_session, 100));

            Assert.AreEqual((1000L, 100L), _awarded[_session.Character.CharacterId]);
        }

        [TestMethod]
        public async Task ALowLevelCharacterEarnsTheBoost()
        {
            _session.Character.Level = 5;
            _session.Character.JobLevel = 15;

            await KillAsync((_session, 100));

            // Level 5 against a level 30 monster loses nothing to the level gap, so only the
            // boosts apply: three times the experience, and twice the job experience at job level 15.
            Assert.AreEqual((3000L, 200L), _awarded[_session.Character.CharacterId]);
        }

        [TestMethod]
        public async Task TwoGroupMembersEachEarnThreeQuartersOfTheKill()
        {
            var other = await TestHelpers.Instance.GenerateSessionAsync();
            JoinGroup(_session, other);
            foreach (var member in new[] { _session, other })
            {
                member.Character.Level = 30;
                member.Character.JobLevel = 30;
            }

            await KillAsync((_session, 60), (other, 40));

            Assert.AreEqual((750L, 75L), _awarded[_session.Character.CharacterId]);
            Assert.AreEqual((750L, 75L), _awarded[other.Character.CharacterId]);
        }

        [TestMethod]
        public async Task ThreeGroupMembersEachEarnSeventyPercentOfTheKill()
        {
            var second = await TestHelpers.Instance.GenerateSessionAsync();
            var third = await TestHelpers.Instance.GenerateSessionAsync();
            JoinGroup(_session, second, third);
            foreach (var member in new[] { _session, second, third })
            {
                member.Character.Level = 30;
                member.Character.JobLevel = 30;
            }

            await KillAsync((_session, 100));

            Assert.AreEqual((700L, 70L), _awarded[third.Character.CharacterId]);
        }

        private static void JoinGroup(params ClientSession[] members)
        {
            var group = new Group(GroupType.Group, TestHelpers.Instance.SessionGroupFactory);
            foreach (var member in members)
            {
                member.Character.JoinGroup(group);
            }
        }

        private Task KillAsync(params (ClientSession Session, int Damage)[] hits)
        {
            var snapshot = new Dictionary<Entity, int>();
            foreach (var (session, damage) in hits)
            {
                snapshot[session.Character.Handle] = damage;
            }

            return _service.DistributeAsync(_monster, null, snapshot);
        }
    }
}
