//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
            await client.SendAsync(TalkToTheStoryteller);
            await client.ExpectAsync<NpcReqPacket>(p => p.TargetId == WorldData.Storyteller);
            var mark = client.Mark();

            await client.SendAsync(TakeTheNextChapter);

            PacketAssert.AreEqual(new ScriptPacket { ScriptId = WorldData.NextChapter, ScriptStepId = WorldData.ChapterStep },
                await client.ExpectSinceAsync<ScriptPacket>(mark));
            PacketAssert.AreEqual(QuestNpc(-1), await client.ExpectSinceAsync<QnpcPacket>(mark));
        }

        private static async Task<TestClient> OnTheLastStepOfTheFirstChapterAsync(string name)
        {
            var player = await Harness.AddPlayerAsync(name, x: WorldData.StorytellerX - 1, y: WorldData.StorytellerY, configure: c =>
            {
                c.Level = 20;
                c.CurrentScriptId = WorldData.FirstChapterLastStep;
            });
            var client = Harness.Client(player);
            var arrival = await client.EnterWorldAsync(player.Slot);
            Assert.IsTrue(arrival.OfType<ScriptPacket>().Any(p => p.ScriptId == WorldData.FirstChapter), "the character is on the first chapter");
            return client;
        }

        private static QnpcPacket QuestNpc(short npcVNum) => new()
        {
            SubPackets =
            [
                new QnpcSubPacket { Unknown0 = npcVNum, Unknown1 = WorldData.Town, Unknown2 = WorldData.NextChapterLevel },
                .. Enumerable.Range(0, 9).Select(_ => new QnpcSubPacket()),
            ],
        };
    }
}
