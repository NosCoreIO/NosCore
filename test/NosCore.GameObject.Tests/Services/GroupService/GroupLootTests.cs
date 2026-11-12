//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.MateExperienceService;
using NosCore.GameObject.Services.MateService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceShareService;
using NosCore.Algorithm.LevelPenaltyService;
using NosCore.Algorithm.LowLevelBoostService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.GroupService
{
    [TestClass]
    public class GroupLootTests
    {
        private TestWorld World;
        private const short GoldVNum = 1046;
        private const short DroppableVNum = 1012;

        private ClientSession _first;
        private ClientSession _second;
        private MapInstance _map;
        private RewardService _service;
        private MonsterComponentBundle _monster;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.ItemList.Add(new ItemDto { Type = NoscorePocketType.Main, VNum = GoldVNum });
            _first = await World.CreateSessionAsync();
            _second = await World.CreateSessionAsync();
            _map = World.GetBaseMap(0);
            var catalog = new Mock<INpcCombatCatalog>();
            catalog.Setup(c => c.GetDrops(1)).Returns(new List<DropDto>
            {
                new() { VNum = DroppableVNum, Amount = 1, DropChance = 10000, MonsterVNum = 1 },
            });
            _service = new RewardService(
                World.GenerateItemProvider(),
                World.MapItemProvider.NotNull(),
                catalog.Object,
                new Mock<IExperienceProgressionService>().Object,
                new ExperienceShareService(),
                new LevelPenaltyService(),
                new LowLevelBoostService(),
                World.GroupLootService,
                new MateProgressionService(new MateExperienceService()),
                World.Create<BuffService>(), NullLogger<RewardService>.Instance);

            _map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 200, MapId = 0, MapX = 5, MapY = 4, VNum = 1 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, Level = 10, MaxHp = 100, MaxMp = 50 } });
            _monster = _map.Monsters.Single();
            foreach (var session in new[] { _first, _second })
            {
                session.Character.PositionX = 6;
                session.Character.PositionY = 4;
            }
        }

        [TestMethod]
        public async Task ASoloKillBelongsToTheTopDamageDealer()
        {
            await new Spec("Without a party, the drops of a kill belong to the character who dealt the most damage")
                .WhenAsync(TheFirstDeals_AndTheSecondDeals_, 30, 70)
                .Then(EveryDropBelongsTo_, "second")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASharingPartyLeavesTheDropsToTheTopDamageDealer()
        {
            await new Spec("A party that shares loot leaves every drop to the top damage dealer, for any member to take")
                .Given(BothAreInAParty)
                .And(TheMasterSharesLootAsAGroup)
                .WhenAsync(TheFirstDeals_AndTheSecondDeals_, 30, 70)
                .Then(EveryDropBelongsTo_, "second")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APartyLootingInTurnHandsEachDropToTheNextMember()
        {
            await new Spec("A party that loots in turn hands successive drops to each member near the kill")
                .Given(BothAreInAParty)
                .WhenAsync(TheFirstDeals_AndTheSecondDeals_, 30, 70)
                .Then(TheItemBelongsTo_, "first")
                .And(TheGoldBelongsTo_, "second")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMemberFarFromTheKillGetsNoTurn()
        {
            await new Spec("A party member far from the kill gets no turn")
                .Given(BothAreInAParty)
                .And(TheSecondStandsFarFromTheKill)
                .WhenAsync(TheFirstDeals_AndTheSecondDeals_, 100, 0)
                .Then(EveryDropBelongsTo_, "first")
                .ExecuteAsync();
        }

        private void BothAreInAParty()
        {
            var group = new Group(GroupType.Group, World.SessionGroupFactory);
            _first.Character.JoinGroup(group);
            _second.Character.JoinGroup(group);
        }

        private void TheMasterSharesLootAsAGroup() => _first.Character.GroupSharing = true;

        private void TheSecondStandsFarFromTheKill() => _second.Character.PositionX = 120;

        private Task TheFirstDeals_AndTheSecondDeals_(int first, int second) =>
            _service.DistributeAsync(_monster, _first.Character, new Dictionary<Entity, int>
            {
                [_first.Character.Handle] = first,
                [_second.Character.Handle] = second,
            });

        private long CharacterId(string who) => (who == "first" ? _first : _second).Character.CharacterId;

        private void EveryDropBelongsTo_(string who)
        {
            var drops = _map.GetMapItemBundles().ToList();
            Assert.AreEqual(2, drops.Count);
            Assert.IsTrue(drops.All(d => d.OwnerId == CharacterId(who)));
        }

        private void TheItemBelongsTo_(string who) =>
            Assert.AreEqual(CharacterId(who), _map.GetMapItemBundles().Single(d => d.VNum == DroppableVNum).OwnerId);

        private void TheGoldBelongsTo_(string who) =>
            Assert.AreEqual(CharacterId(who), _map.GetMapItemBundles().Single(d => d.VNum == GoldVNum).OwnerId);
    }
}
