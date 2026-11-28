//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.Quest;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    // A story chapter an NPC hands out does not follow on by itself: playing the chapter before it to
    // its end names that NPC, and taking the chapter from it clears the name.
    [TestClass]
    public class StoryChapterTests
    {
        private static readonly ScriptClientPacket AcknowledgeTheLastStep = new()
        {
            Type = QuestActionType.Dialog, FirstArgument = WorldData.FirstChapter, SecondArgument = WorldData.ChapterStep
        };

        private static readonly RequestNpcPacket TalkToTheStoryteller = new() { Type = VisualType.Npc, TargetId = WorldData.Storyteller };

        private static readonly NrunPacket TakeTheNextChapter = new()
        {
            Runner = NrunRunnerType.GetQuestFromList, Type = WorldData.NextChapter, VisualType = VisualType.Npc,
            VisualId = WorldData.Storyteller
        };

        [TestMethod]
        public async Task FinishingAChapterNamesTheNpcThatHandsOutTheNext()
        {
            await using var client = await OnTheLastStepOfTheFirstChapterAsync("Reader");
            var mark = client.Mark();

            await client.SendAsync(AcknowledgeTheLastStep);

            PacketAssert.AreEqual(QuestNpc(WorldData.StorytellerVNum), await client.ExpectSinceAsync<QnpcPacket>(mark));
            PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.ClickButtonToSearachNewQuests },
                await client.ExpectSinceAsync<MsgiPacket>(mark));
        }

        [TestMethod]
        public async Task TakingTheChapterFromItsNpcClearsTheName()
        {
            await using var client = await OnTheLastStepOfTheFirstChapterAsync("Listener");
            await client.SendAsync(AcknowledgeTheLastStep);
            await client.ExpectAsync<QnpcPacket>();

            var answered = await AnswerYesAsync(client);

            PacketAssert.AreEqual(new ScriptPacket { ScriptId = WorldData.NextChapter, ScriptStepId = WorldData.ChapterStep },
                await client.ExpectSinceAsync<ScriptPacket>(answered));
            PacketAssert.AreEqual(QuestNpc(null), await client.ExpectSinceAsync<QnpcPacket>(answered));
        }

        [TestMethod]
        public async Task PickingTheChapterAsksBeforeStartingIt()
        {
            await using var client = await OnTheLastStepOfTheFirstChapterAsync("Hesitant");
            await TalkToTheStorytellerAsync(client);
            var mark = client.Mark();

            await client.SendAsync(TakeTheNextChapter);

            PacketAssert.AreEqual(new QnaiPacket { YesPacket = Confirmed(TakeTheNextChapter), Question = Game18NConstString.CannotAbandonMainQuest },
                await client.ExpectSinceAsync<QnaiPacket>(mark));
            Assert.IsTrue(await client.NoneSinceAsync<ScriptPacket>(mark, null, TimeSpan.FromSeconds(1)), "nothing starts before the answer");
        }

        [TestMethod]
        public async Task TakingTheChapterMidwayThroughThePreviousOneSaysAMainQuestIsUnderway()
        {
            await using var client = await OnStepOfTheFirstChapterAsync("Midway", WorldData.FirstChapterOpening);

            var answered = await AnswerYesAsync(client);

            PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.AlreadyAMainQuest },
                await client.ExpectSinceAsync<MsgiPacket>(answered));
            Assert.IsTrue(await client.NoneSinceAsync<ScriptPacket>(answered, null, TimeSpan.FromSeconds(1)), "the chapter does not start");
        }

        private static async Task TalkToTheStorytellerAsync(TestClient client)
        {
            var mark = client.Mark();
            await client.SendAsync(TalkToTheStoryteller);
            await client.ExpectSinceAsync<NpcReqPacket>(mark, p => p.TargetId == WorldData.Storyteller);
        }

        private static async Task<int> AnswerYesAsync(TestClient client)
        {
            await TalkToTheStorytellerAsync(client);
            var mark = client.Mark();
            await client.SendAsync(TakeTheNextChapter);
            var asked = await client.ExpectSinceAsync<QnaiPacket>(mark);
            var answered = client.Mark();
            await client.SendAsync(asked.YesPacket ?? throw new AssertFailedException("the question carries no answer"));
            return answered;
        }

        private static NrunPacket Confirmed(NrunPacket pick) => new()
        {
            Runner = pick.Runner, Type = pick.Type, VisualType = pick.VisualType, VisualId = pick.VisualId, Unknown4 = 1
        };

        private static Task<TestClient> OnTheLastStepOfTheFirstChapterAsync(string name) =>
            OnStepOfTheFirstChapterAsync(name, WorldData.FirstChapterLastStep);

        private static async Task<TestClient> OnStepOfTheFirstChapterAsync(string name, Guid step)
        {
            var player = await Harness.AddPlayerAsync(name, x: WorldData.StorytellerX - 1, y: WorldData.StorytellerY, configure: c =>
            {
                c.Level = 20;
                c.CurrentScriptId = step;
            });
            var client = Harness.Client(player);
            var arrival = await client.EnterWorldAsync(player.Slot);
            Assert.IsTrue(arrival.OfType<ScriptPacket>().Any(p => p.ScriptId == WorldData.FirstChapter), "the character is on the first chapter");
            return client;
        }

        private static QnpcPacket QuestNpc(short? npcVNum) => new()
        {
            SubPackets =
            [
                new QnpcSubPacket { Unknown0 = npcVNum, Unknown1 = WorldData.Town, Unknown2 = WorldData.NextChapterLevel },
                .. Enumerable.Range(0, 9).Select(_ => new QnpcSubPacket()),
            ],
        };
    }
}
