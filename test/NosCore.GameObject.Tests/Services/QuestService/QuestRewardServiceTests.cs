//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.QuestService
{
    [TestClass]
    public class QuestRewardServiceTests
    {
        private const short QuestId = 100;
        private TestWorld World;
        private ClientSession Session;
        private Mock<IExperienceProgressionService> Experience;
        private PlayerComponentBundle Character;
        private readonly List<QuestRewardDto> Rewards = new();
        private readonly List<QuestQuestRewardDto> Links = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.WorldConfiguration.Value.MaxGoldAmount = 1_000_000_000;
            Session = await World.CreateSessionAsync();
            Character = Session.Character;
            Experience = World.Freeze<Mock<IExperienceProgressionService>>();
            World.Inject(Rewards);
            World.Inject(Links);
        }

        private void Prize(QuestRewardDto reward)
        {
            reward.QuestRewardId = (short)(Rewards.Count + 1);
            Rewards.Add(reward);
            Links.Add(new QuestQuestRewardDto { Id = System.Guid.NewGuid(), QuestId = QuestId, QuestRewardId = reward.QuestRewardId });
        }

        private Task GiveAsync() => World.Create<QuestRewardService>().GiveAsync(Character, QuestId);

        private QrPacket Window => Session.LastPackets.OfType<QrPacket>().Single();

        [TestMethod]
        public async Task GoldIsAddedAndPrintedInTheWindow()
        {
            Character.Gold = 100;
            Prize(new QuestRewardDto { RewardType = QuestRewardType.Gold, Amount = 2500 });

            await GiveAsync();

            Assert.AreEqual(2600, Character.Gold);
            Assert.AreEqual(QuestRewardType.Gold, Window.Reward1Type);
            Assert.AreEqual(2500, Window.Reward1Amount);
            Assert.AreEqual(QuestId, Window.QuestId);
        }

        [TestMethod]
        public async Task ExperienceIsAPercentOfTheNamedLevel()
        {
            Prize(new QuestRewardDto { RewardType = QuestRewardType.Experience, Data = 20, Amount = 35 });

            await GiveAsync();

            var expected = World.Create<ExperienceService>().GetExperience(20) * 35 / 100;
            Experience.Verify(e => e.AddExperienceAsync(Character, expected, 0, 0, 0, 0));
        }

        [TestMethod]
        public async Task ANegativeLevelMeansTheCharactersOwnLevel()
        {
            Character.Level = 83;
            Prize(new QuestRewardDto { RewardType = QuestRewardType.Experience, Data = -80, Amount = 4 });

            await GiveAsync();

            var expected = World.Create<ExperienceService>().GetExperience(83) * 4 / 100;
            Experience.Verify(e => e.AddExperienceAsync(Character, expected, 0, 0, 0, 0));
        }

        [TestMethod]
        public async Task JobExperienceWithoutALevelIsAFlatAmount()
        {
            Prize(new QuestRewardDto { RewardType = QuestRewardType.JobExperience, Amount = 800 });

            await GiveAsync();

            Experience.Verify(e => e.AddExperienceAsync(Character, 0, 800, 0, 0, 0));
        }

        [TestMethod]
        public async Task JobExperienceWithALevelIsAPercentOfThatJobLevel()
        {
            Prize(new QuestRewardDto { RewardType = QuestRewardType.JobExperience, Data = 7, Amount = 20 });

            await GiveAsync();

            var expected = World.Create<JobExperienceService>().GetJobExperience(Character.Class, 7) * 20 / 100;
            Experience.Verify(e => e.AddExperienceAsync(Character, 0, expected, 0, 0, 0));
        }

        [TestMethod]
        public async Task ReputationIsAdded()
        {
            Character.Reputation = 1000;
            Prize(new QuestRewardDto { RewardType = QuestRewardType.Reputation, Amount = 500 });

            await GiveAsync();

            Assert.AreEqual(1500, Character.Reputation);
        }

        [TestMethod]
        public async Task OneOfTheListedItemsIsGivenInTheListedAmount()
        {
            Prize(new QuestRewardDto { RewardType = QuestRewardType.EtcMainItem, Data = 1012, SecondData = 1013, Amount = 3 });

            await GiveAsync();

            var given = Window.Reward1Data;
            CollectionAssert.Contains(new[] { 1012, 1013 }, given);
            Assert.AreEqual(3, Character.InventoryService.CountItem((short)given));
            Assert.AreEqual(3, Window.Reward1Amount);
        }

        [TestMethod]
        public async Task AClassItemMatchesTheCharactersClass()
        {
            Character.Class = CharacterClassType.Archer;
            var catalog = World.Freeze<Mock<IItemCatalog>>();
            catalog.Setup(c => c.GetItem(1012)).Returns(new Item { VNum = 1012, Class = 1 << (byte)CharacterClassType.Swordsman });
            catalog.Setup(c => c.GetItem(1013)).Returns(new Item { VNum = 1013, Class = 1 << (byte)CharacterClassType.Archer });
            Prize(new QuestRewardDto { RewardType = QuestRewardType.ClassItem, Data = 1012, SecondData = 1013, Amount = 1 });

            await GiveAsync();

            Assert.AreEqual(1, Character.InventoryService.CountItem(1013));
            Assert.AreEqual(0, Character.InventoryService.CountItem(1012));
        }

        [TestMethod]
        public async Task ALevelRewardNeverLowersTheLevel()
        {
            Character.Level = 30;
            Prize(new QuestRewardDto { RewardType = QuestRewardType.Level, Data = 17 });

            await GiveAsync();

            Assert.AreEqual(30, Character.Level);
        }

        [TestMethod]
        public async Task ALevelRewardRaisesALowerLevel()
        {
            Character.Level = 10;
            Prize(new QuestRewardDto { RewardType = QuestRewardType.LevelAndJobLevel, Data = 20, SecondData = 5 });

            await GiveAsync();

            Assert.AreEqual(20, Character.Level);
            Assert.AreEqual(5, Character.JobLevel);
        }

        [TestMethod]
        public async Task AQuestWithoutPrizesOpensNoWindow()
        {
            await GiveAsync();

            Assert.IsFalse(Session.LastPackets.OfType<QrPacket>().Any());
        }
    }
}
