//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Messaging.Handlers.Quest;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.QuestService.Handlers;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.QuestService
{
    [TestClass]
    public class NpcQuestObjectiveTests
    {
        private const short QuestGiverVNum = 305;
        private const short OtherNpcVNum = 301;
        private const short FirstCrateVNum = 1275;
        private const short SecondCrateVNum = 1276;

        private ClientSession Session = null!;
        private OnNpcTalkedHandler TalkedHandler = null!;
        private QuestDialogHandler DialogHandler = null!;
        private CharacterQuest TrackedQuest = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            Session.Character.Quests = new ConcurrentDictionary<Guid, CharacterQuest>();

            var handlers = new IQuestTypeHandler[]
            {
                new Dialog1QuestHandler(),
                new Dialog2QuestHandler(),
                new InspectQuestHandler(),
                new TransmitGoldQuestHandler(TestHelpers.Instance.GameLanguageLocalizer),
            };
            var questService = new GameObject.Services.QuestService.QuestService(
                new List<ScriptDto>(),
                TestHelpers.Instance.WorldConfiguration,
                new List<QuestDto>(),
                new List<QuestObjectiveDto>(),
                NullLogger<GameObject.Services.QuestService.QuestService>.Instance,
                TestHelpers.Instance.Clock,
                TestHelpers.Instance.LogLanguageLocalizer,
                handlers,
                new Mock<Wolverine.IMessageBus>().Object,
                new List<QuestRewardDto>(),
                new List<QuestQuestRewardDto>(),
                new Mock<GameObject.Services.ItemGenerationService.IItemGenerationService>().Object,
                new Mock<GameObject.Services.ExperienceService.IExperienceProgressionService>().Object);
            TalkedHandler = new OnNpcTalkedHandler(questService, handlers);
            DialogHandler = new QuestDialogHandler(questService, handlers);
        }

        [TestMethod]
        public async Task TalkingToTheNpcCompletesADialog1Quest()
        {
            await new Spec("Talking to the named NPC completes a Dialog1 quest")
                .Given(CharacterHasQuest_With_, QuestType.Dialog1, new[] { Objective(QuestGiverVNum) })
                .WhenAsync(TalkingTo_, QuestGiverVNum)
                .Then(QuestShouldBeComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TalkingToAnotherNpcLeavesADialog1QuestOpen()
        {
            await new Spec("Talking to another NPC leaves a Dialog1 quest open")
                .Given(CharacterHasQuest_With_, QuestType.Dialog1, new[] { Objective(QuestGiverVNum) })
                .WhenAsync(TalkingTo_, OtherNpcVNum)
                .Then(QuestShouldBeOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheDialogButtonCompletesADialog2Quest()
        {
            await new Spec("The n_run 200 button at the named NPC completes the Dialog2 quest")
                .Given(CharacterHasQuest_With_, QuestType.Dialog2, new[] { Objective(QuestGiverVNum) })
                .WhenAsync(PressingTheQuestButtonAt_For_, QuestGiverVNum, TrackedQuestIdPlaceholder)
                .Then(QuestShouldBeComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheDialogButtonAtAnotherNpcLeavesADialog2QuestOpen()
        {
            await new Spec("The n_run 200 button at another NPC leaves the Dialog2 quest open")
                .Given(CharacterHasQuest_With_, QuestType.Dialog2, new[] { Objective(QuestGiverVNum) })
                .WhenAsync(PressingTheQuestButtonAt_For_, OtherNpcVNum, TrackedQuestIdPlaceholder)
                .Then(QuestShouldBeOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheDialogButtonForAnotherQuestLeavesADialog2QuestOpen()
        {
            await new Spec("The n_run 200 button naming another quest leaves the Dialog2 quest open")
                .Given(CharacterHasQuest_With_, QuestType.Dialog2, new[] { Objective(QuestGiverVNum) })
                .WhenAsync(PressingTheQuestButtonAt_For_, QuestGiverVNum, (short)9999)
                .Then(QuestShouldBeOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OpeningTheNpcAloneLeavesADialog2QuestOpen()
        {
            await new Spec("Opening the NPC without pressing the quest button leaves the Dialog2 quest open")
                .Given(CharacterHasQuest_With_, QuestType.Dialog2, new[] { Objective(QuestGiverVNum) })
                .WhenAsync(TalkingTo_, QuestGiverVNum)
                .Then(QuestShouldBeOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InspectingOneOfTwoObjectsKeepsTheQuestOpen()
        {
            await new Spec("Inspecting one of two objects records progress but keeps the Inspect quest open")
                .Given(CharacterHasQuest_With_, QuestType.Inspect, new[] { Objective(FirstCrateVNum, 1752, 1), Objective(SecondCrateVNum, 1753, 1) })
                .WhenAsync(TalkingTo_, FirstCrateVNum)
                .Then(QuestShouldBeOpen)
                .And(ObjectiveForTarget_ShouldBeAt_, FirstCrateVNum, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InspectingEveryObjectCompletesTheQuest()
        {
            await new Spec("Inspecting every object completes the Inspect quest")
                .Given(CharacterHasQuest_With_, QuestType.Inspect, new[] { Objective(FirstCrateVNum, 1752, 1), Objective(SecondCrateVNum, 1753, 1) })
                .AndAsync(TalkingTo_, FirstCrateVNum)
                .WhenAsync(TalkingTo_, SecondCrateVNum)
                .Then(QuestShouldBeComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnObjectToInspectTwiceNeedsTwoInspections()
        {
            await new Spec("An object to inspect twice needs two inspections")
                .Given(CharacterHasQuest_With_, QuestType.Inspect, new[] { Objective(FirstCrateVNum, 1752, 2) })
                .AndAsync(TalkingTo_, FirstCrateVNum)
                .WhenAsync(TalkingTo_, FirstCrateVNum)
                .Then(QuestShouldBeComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OneInspectionOfAnObjectToInspectTwiceKeepsTheQuestOpen()
        {
            await new Spec("One inspection of an object to inspect twice keeps the quest open")
                .Given(CharacterHasQuest_With_, QuestType.Inspect, new[] { Objective(FirstCrateVNum, 1752, 2) })
                .WhenAsync(TalkingTo_, FirstCrateVNum)
                .Then(QuestShouldBeOpen)
                .And(ObjectiveForTarget_ShouldBeAt_, FirstCrateVNum, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InspectingAnotherObjectIsIgnored()
        {
            await new Spec("Inspecting an object the quest does not name is ignored")
                .Given(CharacterHasQuest_With_, QuestType.Inspect, new[] { Objective(FirstCrateVNum, 1752, 1) })
                .WhenAsync(TalkingTo_, SecondCrateVNum)
                .Then(QuestShouldBeOpen)
                .And(ObjectiveForTarget_ShouldBeAt_, FirstCrateVNum, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PayingTheNpcCompletesATransmitGoldQuest()
        {
            await new Spec("Talking to the NPC with enough gold pays it and completes the TransmitGold quest")
                .Given(CharacterHasQuest_With_, QuestType.TransmitGold, new[] { Objective(QuestGiverVNum, 25) })
                .And(CharacterHas_Gold, 300_000L)
                .WhenAsync(TalkingTo_, QuestGiverVNum)
                .Then(QuestShouldBeComplete)
                .And(CharacterShouldHave_Gold, 50_000L)
                .And(AGoldPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NotEnoughGoldIsRefused()
        {
            await new Spec("Talking to the NPC without enough gold is refused and takes nothing")
                .Given(CharacterHasQuest_With_, QuestType.TransmitGold, new[] { Objective(QuestGiverVNum, 25) })
                .And(CharacterHas_Gold, 249_999L)
                .WhenAsync(TalkingTo_, QuestGiverVNum)
                .Then(QuestShouldBeOpen)
                .And(CharacterShouldHave_Gold, 249_999L)
                .And(NotEnoughGoldShouldBeSaid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TalkingToAnotherNpcTakesNoGold()
        {
            await new Spec("Talking to another NPC takes no gold for a TransmitGold quest")
                .Given(CharacterHasQuest_With_, QuestType.TransmitGold, new[] { Objective(QuestGiverVNum, 25) })
                .And(CharacterHas_Gold, 300_000L)
                .WhenAsync(TalkingTo_, OtherNpcVNum)
                .Then(QuestShouldBeOpen)
                .And(CharacterShouldHave_Gold, 300_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACompletedQuestIsNotPaidTwice()
        {
            await new Spec("Talking to the NPC again after paying takes no more gold")
                .Given(CharacterHasQuest_With_, QuestType.TransmitGold, new[] { Objective(QuestGiverVNum, 25) })
                .And(CharacterHas_Gold, 600_000L)
                .AndAsync(TalkingTo_, QuestGiverVNum)
                .WhenAsync(TalkingTo_, QuestGiverVNum)
                .Then(CharacterShouldHave_Gold, 350_000L)
                .ExecuteAsync();
        }

        // Stands for the tracked quest's own id, which is only known once the quest exists.
        private const short TrackedQuestIdPlaceholder = -1;

        private static QuestObjectiveDto Objective(short firstData, int? secondData = null, int? thirdData = null) => new()
        {
            QuestObjectiveId = Guid.NewGuid(),
            FirstData = firstData,
            SecondData = secondData,
            ThirdData = thirdData
        };

        private void CharacterHasQuest_With_(QuestType type, QuestObjectiveDto[] objectives)
        {
            const short questId = 5045;
            foreach (var objective in objectives)
            {
                objective.QuestId = questId;
            }

            TrackedQuest = new CharacterQuest
            {
                Id = Guid.NewGuid(),
                QuestId = questId,
                Quest = new Quest { QuestId = questId, QuestType = type, QuestObjectives = objectives.ToList() }
            };
            Session.Character.Quests.TryAdd(TrackedQuest.Id, TrackedQuest);
        }

        private void CharacterHas_Gold(long gold)
        {
            Session.Character.Gold = gold;
        }

        private Task TalkingTo_(short vnum) =>
            TalkedHandler.Handle(new NpcTalkedEvent(Session.Character, Npc(vnum)));

        private Task PressingTheQuestButtonAt_For_(short vnum, short questId) =>
            DialogHandler.HandleAsync(Session, Npc(vnum), new NrunPacket
            {
                Runner = NrunRunnerType.GetXQuestFromDialog2Quest,
                Type = questId == TrackedQuestIdPlaceholder ? TrackedQuest.QuestId : questId,
                VisualType = VisualType.Npc,
                VisualId = 1
            });

        private static INonPlayableEntity Npc(short vnum)
        {
            var npc = new Mock<INonPlayableEntity>();
            npc.SetupGet(n => n.NpcMonster).Returns(new NpcMonsterDto { NpcMonsterVNum = vnum });
            return npc.Object;
        }

        private void QuestShouldBeComplete()
        {
            Assert.IsNotNull(TrackedQuest.ObjectivesCompletedOn);
        }

        private void QuestShouldBeOpen()
        {
            Assert.IsNull(TrackedQuest.ObjectivesCompletedOn);
            Assert.IsNull(TrackedQuest.CompletedOn);
        }

        private void ObjectiveForTarget_ShouldBeAt_(short vnum, int expected)
        {
            var objective = TrackedQuest.Quest.QuestObjectives.Single(o => o.FirstData == vnum);
            Assert.AreEqual(expected, TrackedQuest.ObjectiveProgress.TryGetValue(objective.QuestObjectiveId, out var c) ? c : 0);
        }

        private void CharacterShouldHave_Gold(long gold)
        {
            Assert.AreEqual(gold, Session.Character.Gold);
        }

        private void AGoldPacketShouldBeSent()
        {
            Assert.IsTrue(Session.LastPackets.OfType<GoldPacket>().Any(p => p.Gold == Session.Character.Gold));
        }

        private void NotEnoughGoldShouldBeSaid()
        {
            Assert.IsTrue(Session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.NotEnoughGold));
        }
    }
}
