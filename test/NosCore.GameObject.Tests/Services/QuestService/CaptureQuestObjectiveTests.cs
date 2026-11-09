//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.MateExperienceService;
using NosCore.Core.Services.IdService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Quest;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.QuestService.Handlers;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using Mate = NosCore.GameObject.Services.MateService.Mate;
using MateServiceImpl = NosCore.GameObject.Services.MateService.MateService;

namespace NosCore.GameObject.Tests.Services.QuestService
{
    [TestClass]
    public class CaptureQuestObjectiveTests
    {
        private const short ChickenVNum = 333;
        private const short DanderVNum = 24;

        private static readonly SkillInfo CaptureSkill = new(209, 16, 150, 15, -1, 0, 0, default, 1, 0, 0, 0, 0, 0,
            new List<BCardDto>
            {
                new() { Type = BCardEffect.CaptureCaptureAnimal.Type(), SubType = BCardEffect.CaptureCaptureAnimal.SubType() }
            });

        private ClientSession Session = null!;
        private CaptureService Captures = null!;
        private InlineOrDeferredMessageBus<EntityCapturedEvent> Bus = null!;
        private CharacterQuest TrackedQuest = null!;
        private readonly Dictionary<short, NpcMonsterDto> Creatures = new()
        {
            [ChickenVNum] = new NpcMonsterDto { NpcMonsterVNum = ChickenVNum, Level = 1, MaxHp = 100, MaxMp = 10, Name = Named("Chicken") },
            [DanderVNum] = new NpcMonsterDto { NpcMonsterVNum = DanderVNum, Level = 1, MaxHp = 100, MaxMp = 10, Name = Named("Baby Dander") },
        };
        private int _nextMonsterId = 9001;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>();

            var questService = new GameObject.Services.QuestService.QuestService(
                new List<ScriptDto>(),
                TestHelpers.Instance.WorldConfiguration,
                new List<QuestDto>(),
                new List<QuestObjectiveDto>(),
                NullLogger<GameObject.Services.QuestService.QuestService>.Instance,
                TestHelpers.Instance.Clock,
                TestHelpers.Instance.LogLanguageLocalizer,
                new IQuestTypeHandler[] { new CaptureQuestHandler(), new CaptureWithoutGettingTheMonsterQuestHandler() },
                new Mock<Wolverine.IMessageBus>().Object,
                new List<QuestRewardDto>(),
                new List<QuestQuestRewardDto>(),
                new Mock<GameObject.Services.ItemGenerationService.IItemGenerationService>().Object,
                new Mock<GameObject.Services.ExperienceService.IExperienceProgressionService>().Object);
            var capturedHandler = new OnEntityCapturedHandler(questService, NullLogger<OnEntityCapturedHandler>.Instance);
            Bus = new InlineOrDeferredMessageBus<EntityCapturedEvent>(capturedHandler.Handle);

            var random = new Mock<IRandomProvider>();
            random.Setup(r => r.Next(0, 100)).Returns(0);
            var mateService = new MateServiceImpl(TestHelpers.Instance.MateDao, Creatures.Values.ToList(),
                new IdService<Mate>(2000000), new MateExperienceService(), NullLogger<MateServiceImpl>.Instance);
            Captures = new CaptureService(TestHelpers.Instance.MateDao, mateService, Bus.Object,
                TestHelpers.Instance.Clock, random.Object, TestHelpers.Instance.DistanceCalculator);
        }

        [TestMethod]
        public async Task CapturingTheNamedMonsterRecordsProgress()
        {
            await new Spec("Capturing the named monster records progress on a Capture quest")
                .Given(CharacterHasQuest_Needing_Of_, QuestType.Capture, 2, ChickenVNum)
                .WhenAsync(Capturing_, ChickenVNum)
                .Then(ObjectiveShouldBeAt_, 1)
                .And(QuestShouldBeOpen)
                .And(TheQuestWindowShouldBeRefreshed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CapturingTheLastMonsterCompletesTheQuest()
        {
            await new Spec("Capturing the last monster needed completes a Capture quest")
                .Given(CharacterHasQuest_Needing_Of_, QuestType.Capture, 2, ChickenVNum)
                .AndAsync(Capturing_, ChickenVNum)
                .WhenAsync(Capturing_, ChickenVNum)
                .Then(ObjectiveShouldBeAt_, 2)
                .And(QuestShouldBeComplete)
                .And(QuestCompleteShouldBeSaid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CapturingAnotherMonsterIsIgnored()
        {
            await new Spec("Capturing a monster the quest does not name is ignored")
                .Given(CharacterHasQuest_Needing_Of_, QuestType.Capture, 2, ChickenVNum)
                .AndAsync(Capturing_, DanderVNum)
                .WhenAsync(Capturing_, ChickenVNum)
                .Then(ObjectiveShouldBeAt_, 1)
                .And(QuestShouldBeOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACaptureQuestKeepsThePet()
        {
            await new Spec("A Capture quest leaves the captured monster as a pet")
                .Given(CharacterHasQuest_Needing_Of_, QuestType.Capture, 1, ChickenVNum)
                .WhenAsync(Capturing_, ChickenVNum)
                .Then(QuestShouldBeComplete)
                .And(CharacterShouldHave_PetsOf_, 1, ChickenVNum)
                .And(TheCaptureShouldBeAnnouncedAs_, Game18NConstString.CaptureSuccessful)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACaptureWithoutGettingTheMonsterQuestMakesNoPet()
        {
            await new Spec("A CaptureWithoutGettingTheMonster quest takes the monster and makes no pet")
                .Given(CharacterHasQuest_Needing_Of_, QuestType.CaptureWithoutGettingTheMonster, 1, ChickenVNum)
                .WhenAsync(Capturing_, ChickenVNum)
                .Then(QuestShouldBeComplete)
                .And(CharacterShouldHave_PetsOf_, 0, ChickenVNum)
                .And(TheCaptureShouldBeAnnouncedAs_, Game18NConstString.CaptureSuccessfulQuest)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACaptureWithoutGettingTheMonsterQuestRecordsProgress()
        {
            await new Spec("A CaptureWithoutGettingTheMonster quest records each capture without making a pet")
                .Given(CharacterHasQuest_Needing_Of_, QuestType.CaptureWithoutGettingTheMonster, 2, ChickenVNum)
                .WhenAsync(Capturing_, ChickenVNum)
                .Then(ObjectiveShouldBeAt_, 1)
                .And(QuestShouldBeOpen)
                .And(CharacterShouldHave_PetsOf_, 0, ChickenVNum)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherMonsterStillBecomesAPetDuringACaptureWithoutGettingTheMonsterQuest()
        {
            await new Spec("A monster the CaptureWithoutGettingTheMonster quest does not name still becomes a pet")
                .Given(CharacterHasQuest_Needing_Of_, QuestType.CaptureWithoutGettingTheMonster, 2, ChickenVNum)
                .AndAsync(Capturing_, DanderVNum)
                .WhenAsync(Capturing_, ChickenVNum)
                .Then(ObjectiveShouldBeAt_, 1)
                .And(CharacterShouldHave_PetsOf_, 1, DanderVNum)
                .And(CharacterShouldHave_PetsOf_, 0, ChickenVNum)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CapturesBeyondTheObjectiveBecomePets()
        {
            await new Spec("Once the CaptureWithoutGettingTheMonster objective is met, the next capture becomes a pet")
                .Given(CharacterHasQuest_Needing_Of_, QuestType.CaptureWithoutGettingTheMonster, 1, ChickenVNum)
                .AndAsync(Capturing_, ChickenVNum)
                .WhenAsync(Capturing_, ChickenVNum)
                .Then(ObjectiveShouldBeAt_, 1)
                .And(CharacterShouldHave_PetsOf_, 1, ChickenVNum)
                .ExecuteAsync();
        }

        private static I18NString Named(string name)
        {
            var text = new I18NString();
            text[RegionType.EN] = name;
            return text;
        }

        private void CharacterHasQuest_Needing_Of_(QuestType type, int amount, short monsterVNum)
        {
            const short questId = 1501;
            var objective = new QuestObjectiveDto
            {
                QuestObjectiveId = Guid.NewGuid(),
                QuestId = questId,
                FirstData = monsterVNum,
                SecondData = amount,
                ThirdData = amount
            };
            TrackedQuest = new CharacterQuest
            {
                Id = Guid.NewGuid(),
                QuestId = questId,
                Quest = new Quest { QuestId = questId, QuestType = type, QuestObjectives = new List<QuestObjectiveDto> { objective } }
            };
            Session.Character.Quests.TryAdd(TrackedQuest.Id, TrackedQuest);
        }

        private async Task Capturing_(short monsterVNum)
        {
            var map = Session.Character.MapInstance;
            var monster = new MonsterComponentBundle(
                map.EcsWorld.CreateMonster(_nextMonsterId++, Creatures[monsterVNum], map, 12, 14, 2, 12, 14, false, true, false),
                map.EcsWorld) { Hp = 40 };
            Session.Character.PositionX = 12;
            Session.Character.PositionY = 13;
            Session.LastPackets.Clear();

            await Captures.TryCaptureAsync(Session.Character, monster, CaptureSkill);
            await Bus.DeliverPublishedAsync();
        }

        private void ObjectiveShouldBeAt_(int expected)
        {
            var objective = TrackedQuest.Quest.QuestObjectives.Single();
            Assert.AreEqual(expected, TrackedQuest.ObjectiveProgress.GetValueOrDefault(objective.QuestObjectiveId));
        }

        private void QuestShouldBeOpen()
        {
            Assert.IsNull(TrackedQuest.ObjectivesCompletedOn);
            Assert.IsNull(TrackedQuest.CompletedOn);
        }

        private void QuestShouldBeComplete()
        {
            Assert.IsNotNull(TrackedQuest.ObjectivesCompletedOn);
        }

        private void TheQuestWindowShouldBeRefreshed()
        {
            Assert.IsTrue(Session.LastPackets.OfType<QstiPacket>().Any(p => p.QuestSubPacket?.QuestId == TrackedQuest.QuestId));
        }

        private void QuestCompleteShouldBeSaid()
        {
            Assert.IsTrue(Session.LastPackets.OfType<MsgiPacket>().Any(p => p.Message == Game18NConstString.QuestComplete));
        }

        private void CharacterShouldHave_PetsOf_(int expected, short monsterVNum)
        {
            Assert.AreEqual(expected, Session.Character.Mates.Values.Count(m => m.VNum == monsterVNum));
        }

        private void TheCaptureShouldBeAnnouncedAs_(Game18NConstString message)
        {
            Assert.IsTrue(Session.LastPackets.OfType<MsgiPacket>().Any(p => p.Message == message));
        }
    }
}
