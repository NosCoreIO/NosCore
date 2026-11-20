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
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class NamedQuestButtonTests
    {
        [TestMethod]
        public async Task TheMerlingsCurseButtonHandsOutTheQuestItNames()
        {
            var player = await Harness.AddPlayerAsync("Alice", x: WorldData.FishermanX, y: WorldData.FishermanY + 1);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            await alice.SendAsync(new RequestNpcPacket { Type = VisualType.Npc, TargetId = WorldData.Fisherman });
            await alice.ExpectAsync<NpcReqPacket>(p => p.Type == VisualType.Npc && p.TargetId == WorldData.Fisherman
                && p.Data == WorldData.FishermanDialog, "the fisherman's dialog");
            var asked = alice.Mark();

            await alice.SendAsync(new NrunPacket
            {
                Runner = NrunRunnerType.GetQuestTheMerlingsCurse, Type = WorldData.FishermanQuest, VisualType = VisualType.Npc,
                VisualId = WorldData.Fisherman
            });

            await alice.ExpectSinceAsync<QstlistPacket>(asked,
                p => p.QuestSubPackets.Any(q => q.QuestId == WorldData.FishermanQuest), "the named quest in the quest window");
        }
    }
}
