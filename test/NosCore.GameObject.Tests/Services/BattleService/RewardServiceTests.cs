//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class RewardServiceTests
    {
        private const short GoldVNum = 1046;
        private const short DroppableVNum = 1012;

        private ClientSession _first = null!;
        private ClientSession _second = null!;
        private MapInstance _map = null!;
        private Mock<IExperienceProgressionService> _experience = null!;
        private Mock<INpcCombatCatalog> _catalog = null!;
        private RewardService _service = null!;
        private MonsterComponentBundle _monster;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            TestHelpers.Instance.ItemList.Add(new ItemDto { Type = NoscorePocketType.Main, VNum = GoldVNum });
            _first = await TestHelpers.Instance.GenerateSessionAsync();
            _second = await TestHelpers.Instance.GenerateSessionAsync();
            _map = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            _experience = new Mock<IExperienceProgressionService>();
            _catalog = new Mock<INpcCombatCatalog>();
            _catalog.Setup(c => c.GetDrops(It.IsAny<short>())).Returns(new List<DropDto>());
            _service = new RewardService(
                TestHelpers.Instance.GenerateItemProvider(),
                TestHelpers.Instance.MapItemProvider!,
                _catalog.Object,
                _experience.Object,
                NullLogger<RewardService>.Instance);

            var template = new NpcMonsterDto
            {
                NpcMonsterVNum = 1, Level = 10, MaxHp = 100, MaxMp = 50, Xp = 1000, JobXp = 100,
            };
            _map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 200, MapId = 0, MapX = 5, MapY = 4, VNum = 1 } },
                new List<NpcMonsterDto> { template });
            _monster = _map.Monsters.Single();
        }

        private Dictionary<Entity, int> Damage(int firstShare, int secondShare) => new()
        {
            [_first.Character.Handle] = firstShare,
            [_second.Character.Handle] = secondShare,
        };

        private void MonsterDrops(short vnum, int chance) =>
            _catalog.Setup(c => c.GetDrops(1)).Returns(new List<DropDto>
            {
                new() { VNum = vnum, Amount = 1, DropChance = chance, MonsterVNum = 1 },
            });

        [TestMethod]
        public async Task ExperienceIsSharedByTheDamageEachPlayerDealt()
        {
            var firstId = _first.Character.VisualId;
            var secondId = _second.Character.VisualId;

            await _service.DistributeAsync(_monster, _first.Character, Damage(30, 70));

            _experience.Verify(e => e.AddExperienceAsync(
                It.Is<PlayerComponentBundle>(p => p.VisualId == firstId), 300, 30, 0, 0, It.IsAny<int>()), Times.Once);
            _experience.Verify(e => e.AddExperienceAsync(
                It.Is<PlayerComponentBundle>(p => p.VisualId == secondId), 700, 70, 0, 0, It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        public async Task ANonPlayerInTheHitListEarnsNothing()
        {
            var damage = Damage(50, 50);
            damage[_monster.Handle] = 100;

            await _service.DistributeAsync(_monster, _first.Character, damage);

            _experience.Verify(e => e.AddExperienceAsync(
                It.IsAny<PlayerComponentBundle>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task ADropWithACertainChanceLandsWhereTheMonsterFell()
        {
            MonsterDrops(DroppableVNum, chance: 10000);

            await _service.DistributeAsync(_monster, _first.Character, Damage(1, 1));

            var drops = _map.GetMapItemBundles().Where(i => i.VNum == DroppableVNum).ToList();
            Assert.AreEqual(1, drops.Count);
            Assert.AreEqual((short)5, drops[0].PositionX);
            Assert.AreEqual((short)4, drops[0].PositionY);
        }

        [TestMethod]
        public async Task ADropWithNoChanceNeverLands()
        {
            MonsterDrops(DroppableVNum, chance: 0);

            await _service.DistributeAsync(_monster, _first.Character, Damage(1, 1));

            Assert.IsFalse(_map.GetMapItemBundles().Any(i => i.VNum == DroppableVNum));
        }

        [TestMethod]
        public async Task GoldFallsWithinTheMonstersLevelBand()
        {
            await _service.DistributeAsync(_monster, _first.Character, Damage(1, 1));

            var gold = _map.GetMapItemBundles().Single(i => i.VNum == GoldVNum);
            Assert.IsTrue(gold.Amount is >= 60 and <= 120, $"level 10 pays 6..12 per level, got {gold.Amount}");
        }

        [TestMethod]
        public async Task NothingIsRewardedWhenNobodyDamagedTheMonster()
        {
            MonsterDrops(DroppableVNum, chance: 10000);

            await _service.DistributeAsync(_monster, _first.Character, new Dictionary<Entity, int>());

            Assert.AreEqual(0, _map.MapItemCount);
            _experience.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task TheHitListIsClearedOnceTheRewardsAreGiven()
        {
            _monster.HitList[_first.Character.Handle] = 10;

            await _service.DistributeAsync(_monster, _first.Character, Damage(1, 1));

            Assert.AreEqual(0, _monster.HitList.Count);
        }
    }
}
