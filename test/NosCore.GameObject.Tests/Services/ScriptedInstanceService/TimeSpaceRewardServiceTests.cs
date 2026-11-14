//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpaceRewardServiceTests
    {
        private const short Bonus = 1013;
        private const short Special = 1015;
        private const short Drawn = 1012;
        private const long FamilyId = 7;

        private TestWorld World;
        private ClientSession _session;
        private ScriptedInstanceDefinition _definition;
        private ScriptedInstanceRun _run;
        private Mock<IScriptedInstanceService> _instances;
        private Mock<IFamilyContributionService> _families;
        private readonly List<short> _delivered = [];
        private bool _opened;

        private sealed class Script : InstanceScript
        {
            public bool Wins { get; init; }

            public Script(ScriptedInstanceDefinition definition) => Definition = definition;

            public override void OnFirstEnter(IInstanceRun run)
            {
                if (Wins)
                {
                    run.Succeed();
                }
                else
                {
                    run.Fail();
                }
            }
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.WorldConfiguration.Value.MaxGoldAmount = 1_000_000;
            _session = await World.CreateSessionAsync();
            _definition = InstanceDefinitionBuilder.Named(1, "timespace", "timespace")
                .WithGold(500)
                .WithReputation(30)
                .WithFamilyExperience(200)
                .WithReward(Bonus, 2)
                .WithSpecialReward(Special, 1)
                .WithDraw(Drawn, 3)
                .Build();
            _instances = World.Freeze<Mock<IScriptedInstanceService>>();
            _families = World.Freeze<Mock<IFamilyContributionService>>();
            var delivery = World.Freeze<Mock<IItemDeliveryService>>();
            delivery.Setup(d => d.DeliverAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<IItemInstance>()))
                .Callback((PlayerComponentBundle _, IItemInstance item) => _delivered.Add(item.ItemVNum))
                .ReturnsAsync(true);
        }

        [TestMethod]
        public async Task AWonTimeSpacePaysOutItsChest()
        {
            await new Spec("Opening the chest of a won time-space pays its gold, reputation and items")
                .GivenAsync(TheTimeSpaceWasWon)
                .WhenAsync(ThePlayerOpensTheChest)
                .Then(TheChestShouldOpen)
                .And(ThePlayerShouldHave_Gold_Reputation, 500L, 30L)
                .And(ThePlayerShouldReceiveTheBonusSpecialAndDrawnItems)
                .And(TheChestShouldShowWhatWasWon)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFamilyMemberEarnsTheFamilyItsExperience()
        {
            await new Spec("Opening the chest gives the player's family the time-space's family experience")
                .Given(ThePlayerIsInAFamily)
                .AndAsync(TheTimeSpaceWasWon)
                .WhenAsync(ThePlayerOpensTheChest)
                .Then(TheFamilyShouldEarn_Experience, 200)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APlayerWithoutAFamilyEarnsNoFamilyExperience()
        {
            await new Spec("A player in no family earns no family experience from the chest")
                .GivenAsync(TheTimeSpaceWasWon)
                .WhenAsync(ThePlayerOpensTheChest)
                .Then(NoFamilyShouldEarnExperience)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheChestOpensOnce()
        {
            await new Spec("A second click on the chest gives nothing more")
                .GivenAsync(TheTimeSpaceWasWon)
                .AndAsync(ThePlayerOpensTheChest)
                .WhenAsync(ThePlayerOpensTheChest)
                .Then(TheChestShouldNotOpen)
                .And(ThePlayerShouldHave_Gold_Reputation, 500L, 30L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFailedTimeSpaceHasNoChest()
        {
            await new Spec("A time-space that ended in failure pays nothing")
                .GivenAsync(TheTimeSpaceWasLost)
                .WhenAsync(ThePlayerOpensTheChest)
                .Then(TheChestShouldNotOpen)
                .And(ThePlayerShouldHave_Gold_Reputation, 0L, 0L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoChestOutsideATimeSpace()
        {
            await new Spec("A player who is in no time-space has no chest to open")
                .WhenAsync(ThePlayerOpensTheChest)
                .Then(TheChestShouldNotOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GoldStopsAtTheLimit()
        {
            await new Spec("The chest's gold never takes the player past the gold limit")
                .Given(ThePlayerHolds_Gold, 999_800L)
                .AndAsync(TheTimeSpaceWasWon)
                .WhenAsync(ThePlayerOpensTheChest)
                .Then(ThePlayerShouldHave_Gold_Reputation, 1_000_000L, 30L)
                .ExecuteAsync();
        }

        [TestMethod]
        public void TheEntrancePanelListsTheChest()
        {
            var rbr = new GameObject.Services.ScriptedInstanceService.ScriptedInstance { Logic = new Script(_definition) }.GenerateRbr();

            Assert.AreEqual(Drawn, rbr.DrawRewards?[0]?.ItemId);
            Assert.AreEqual((short?)3, rbr.DrawRewards?[0]?.Quantity);
            Assert.IsNull(rbr.DrawRewards?[1]?.ItemId);
            Assert.AreEqual(Special, rbr.SpecialRewards?[0]?.ItemId);
            Assert.AreEqual(Bonus, rbr.BonusRewards?[0]?.ItemId);
            Assert.AreEqual((short?)2, rbr.BonusRewards?[0]?.Quantity);
        }

        private Task TheTimeSpaceWasWon() => EndAsync(wins: true);

        private Task TheTimeSpaceWasLost() => EndAsync(wins: false);

        private async Task EndAsync(bool wins)
        {
            var room = World.MapInstanceGeneratorService.CreateMapInstance(
                new GameObject.Map.Map
                {
                    MapId = 2004,
                    NameI18NKey = "instanceRoom",
                    Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
            var script = new Script(_definition) { Wins = wins };
            _run = new ScriptedInstanceRun(
                new TimeSpaceRunHost(
                    new GameObject.Services.ScriptedInstanceService.ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, Logic = script },
                    new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), new Mock<Wolverine.IMessageBus>().Object),
                script,
                new Dictionary<int, MapInstance> { [1] = room },
                new Dictionary<short, NpcMonsterDto>(),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, TimeSpan.FromSeconds(30));
            _session.Character.MapInstance = room;
            _instances.Setup(s => s.GetRun(room.MapInstanceId)).Returns(_run);
            await _run.StartAsync();
        }

        private void ThePlayerIsInAFamily()
        {
            _session.Character.Family = new Family { FamilyId = FamilyId };
        }

        private void TheFamilyShouldEarn_Experience(int experience)
        {
            var characterId = _session.Character.CharacterId;
            var name = _session.Character.Name;
            _families.Verify(f => f.ExperienceEarnedAsync(FamilyId, characterId, name, experience), Times.Once);
        }

        private void NoFamilyShouldEarnExperience()
        {
            _families.Verify(f => f.ExperienceEarnedAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<int>()), Times.Never);
        }

        private void ThePlayerHolds_Gold(long gold)
        {
            _session.Character.Gold = gold;
        }

        private async Task ThePlayerOpensTheChest()
        {
            _session.LastPackets.Clear();
            _opened = await World.Create<TimeSpaceRewardService>().OpenChestAsync(_session);
        }

        private void TheChestShouldOpen()
        {
            Assert.IsTrue(_opened);
        }

        private void TheChestShouldNotOpen()
        {
            Assert.IsFalse(_opened);
            Assert.IsFalse(_session.LastPackets.OfType<RepayPacket>().Any());
        }

        private void ThePlayerShouldHave_Gold_Reputation(long gold, long reputation)
        {
            Assert.AreEqual(gold, _session.Character.Gold);
            Assert.AreEqual(reputation, _session.Character.Reputation);
        }

        private void ThePlayerShouldReceiveTheBonusSpecialAndDrawnItems()
        {
            CollectionAssert.AreEquivalent(new[] { Bonus, Special, Drawn }, _delivered);
        }

        private void TheChestShouldShowWhatWasWon()
        {
            var repay = _session.LastPackets.OfType<RepayPacket>().Single();
            Assert.AreEqual(Bonus, repay.BonusRewards.Single().ItemId);
            Assert.AreEqual(Special, repay.SpecialRewards.Single().ItemId);
            Assert.AreEqual(Drawn, repay.DrawnReward.ItemId);
            Assert.AreEqual((short)3, repay.DrawnReward.Amount);
        }
    }
}
