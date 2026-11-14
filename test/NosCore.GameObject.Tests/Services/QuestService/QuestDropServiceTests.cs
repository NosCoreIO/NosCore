//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.QuestService
{
    [TestClass]
    public class QuestDropServiceTests
    {
        private const short QuestItem = 1013;
        private const short MonsterVNum = 25;
        private TestWorld World;
        private ClientSession Session;
        private readonly NpcMonsterDto Monster = new() { NpcMonsterVNum = MonsterVNum, Level = 5 };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
        }

        private CharacterQuest Collect(short monster, int amount, int chance, QuestType type = QuestType.Collect)
        {
            var quest = new CharacterQuest
            {
                QuestId = 1600,
                Quest = new Quest
                {
                    QuestId = 1600, QuestType = type,
                    QuestObjectives = [new QuestObjectiveDto { QuestObjectiveId = Guid.NewGuid(), QuestId = 1600, FirstData = monster, SecondData = QuestItem, ThirdData = amount, FourthData = chance }],
                },
            };
            Session.Character.Quests.TryAdd(Guid.NewGuid(), quest);
            return quest;
        }

        private System.Collections.Generic.IReadOnlyList<short> Roll() =>
            World.Create<QuestDropService>().RollDrops(Session.Character, Monster, 1);

        [TestMethod]
        public void TheNamedMonsterDropsTheItemOfAnActiveCollectQuest()
        {
            Collect(MonsterVNum, 2, chance: 10000);

            CollectionAssert.AreEqual(new[] { QuestItem }, Roll().ToArray());
        }

        [TestMethod]
        public void AnotherMonsterDropsNothing()
        {
            Collect(26, 2, chance: 10000);

            Assert.AreEqual(0, Roll().Count);
        }

        [TestMethod]
        public void AChanceOfZeroNeverDrops()
        {
            Collect(MonsterVNum, 2, chance: 0);

            Assert.AreEqual(0, Roll().Count);
        }

        [TestMethod]
        public void NothingDropsOnceTheBagHoldsEnough()
        {
            Collect(MonsterVNum, 2, chance: 10000);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(World.GenerateItemProvider().Create(QuestItem, 2), Session.Character.CharacterId));

            Assert.AreEqual(0, Roll().Count);
        }

        [TestMethod]
        public void AFinishedQuestDropsNothing()
        {
            Collect(MonsterVNum, 2, chance: 10000).CompletedOn = NodaTime.Instant.FromUnixTimeSeconds(1);

            Assert.AreEqual(0, Roll().Count);
        }

        [TestMethod]
        public void AnItemTheMonsterAlreadyDropsIsNotRolledAgain()
        {
            World.Freeze<Mock<INpcCombatCatalog>>().Setup(c => c.GetDrops(MonsterVNum))
                .Returns(new List<DropDto> { new() { VNum = QuestItem, Amount = 1, DropChance = 10000, MonsterVNum = MonsterVNum } });
            Collect(MonsterVNum, 2, chance: 10000);

            Assert.AreEqual(0, Roll().Count);
        }

        [TestMethod]
        public void ARaidCollectQuestDropsOnEveryKill()
        {
            Collect(MonsterVNum, 2, chance: 2, QuestType.CollectInRaid);

            CollectionAssert.AreEqual(new[] { QuestItem }, Roll().ToArray());
        }

        [TestMethod]
        public void ARaidCollectQuestStopsDroppingOnceItsCounterIsFull()
        {
            var quest = Collect(MonsterVNum, 2, chance: 2, QuestType.CollectInRaid);
            quest.ObjectiveProgress[quest.Quest.QuestObjectives[0].QuestObjectiveId] = 2;

            Assert.AreEqual(0, Roll().Count);
        }

        [TestMethod]
        public void AHuntQuestDropsNothing()
        {
            Collect(MonsterVNum, 2, chance: 10000, QuestType.Hunt);

            Assert.AreEqual(0, Roll().Count);
        }
    }
}
