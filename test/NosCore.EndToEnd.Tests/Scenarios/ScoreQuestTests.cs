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
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class ScoreQuestTests
    {
        private static async Task<TestClient> AtTheScoreKeeperAsync()
        {
            var player = await Harness.AddPlayerAsync("Alice", x: WorldData.ScoreKeeperX, y: WorldData.ScoreKeeperY + 1);
            var client = Harness.Client(player);
            try
            {
                await client.EnterWorldAsync(player.Slot);
                await client.SendAsync(new RequestNpcPacket { Type = VisualType.Npc, TargetId = WorldData.ScoreKeeper });
                await client.ExpectAsync<NpcReqPacket>(p => p.Type == VisualType.Npc && p.TargetId == WorldData.ScoreKeeper
                    && p.Data == WorldData.ScoreKeeperDialog, "the score keeper's dialog");
                return client;
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }
        }

        private static NrunPacket ScoreQuestButton(short option) => new()
        {
            Runner = NrunRunnerType.GetQuestHigherTimeSpaceScore, Type = option, VisualType = VisualType.Npc,
            VisualId = WorldData.ScoreKeeper
        };

        [TestMethod]
        public async Task AScoreQuestButtonHandsOutTheQuestOfItsLevelRange()
        {
            await using var alice = await AtTheScoreKeeperAsync();
            var asked = alice.Mark();

            await alice.SendAsync(ScoreQuestButton(0));

            await alice.ExpectSinceAsync<QstlistPacket>(asked,
                p => p.QuestSubPackets.Any(q => q.QuestId == WorldData.AnyLevelScoreQuest && q.QuestType == QuestType.TsPoint),
                "the score quest in the quest window");
        }

        [TestMethod]
        public async Task AScoreQuestButtonSentToAnotherNpcIsRefused()
        {
            var player = await Harness.AddPlayerAsync("Alice", x: WorldData.BrokerX, y: WorldData.BrokerY + 1);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            await alice.SendAsync(new RequestNpcPacket { Type = VisualType.Npc, TargetId = WorldData.Broker });
            await alice.ExpectAsync<NpcReqPacket>(p => p.Type == VisualType.Npc && p.TargetId == WorldData.Broker, "the broker's dialog");
            var asked = alice.Mark();

            await alice.SendAsync(new NrunPacket
            {
                Runner = NrunRunnerType.GetQuestHigherTimeSpaceScore, Type = 0, VisualType = VisualType.Npc,
                VisualId = WorldData.Broker
            });

            Assert.IsTrue(await alice.NoneSinceAsync<QstlistPacket>(asked,
                p => p.QuestSubPackets.Any(q => q.QuestId == WorldData.AnyLevelScoreQuest), TimeSpan.FromSeconds(2)));
        }

        [TestMethod]
        public async Task AButtonForAHigherLevelRangeIsRefused()
        {
            await using var alice = await AtTheScoreKeeperAsync();
            var asked = alice.Mark();

            await alice.SendAsync(ScoreQuestButton(1));

            PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.LevelTooLow },
                await alice.ExpectSinceAsync<MsgiPacket>(asked, p => p.Message == Game18NConstString.LevelTooLow, "the level refusal"));
            Assert.IsTrue(await alice.NoneSinceAsync<QstlistPacket>(asked,
                p => p.QuestSubPackets.Any(q => q.QuestId == WorldData.HighLevelScoreQuest), TimeSpan.FromSeconds(1)));
        }
    }
}
