//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Npcs;
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class RelictTests
    {
        private const long Gold = 25_000;

        private static readonly RequestNpcPacket TalkToTheArchaeologist = new() { Type = VisualType.Npc, TargetId = WorldData.Archaeologist };

        // What the client's menu sends for the examination service: the window it wants, by number.
        private static readonly NrunPacket AskForTheExaminationWindow = new()
        {
            Runner = NrunRunnerType.ProbabilityUIs, Type = (short)WindowType.ExamineRelics,
            VisualType = VisualType.Npc, VisualId = WorldData.Archaeologist
        };

        private static readonly GuriPacket ConfirmTheFee = new() { Type = GuriPacketType.ActVRelic, Argument = WorldData.RelictFee };

        private static Func<NpcReqPacket, bool> ArchaeologistsDialog =>
            packet => packet.Type == VisualType.Npc && packet.TargetId == WorldData.Archaeologist;

        private static async Task<TestClient> HoldingARelictAsync()
        {
            var player = await Harness.AddPlayerAsync("Alice", x: WorldData.ArchaeologistX, y: WorldData.ArchaeologistY - 1,
                configure: c => c.Gold = Gold);
            await Harness.GiveAsync(player, WorldData.Relict, 1, pocket: NoscorePocketType.Main);
            var client = Harness.Client(player);
            try
            {
                await client.EnterWorldAsync(player.Slot);
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }

            return client;
        }

        private static async Task OpenTheExaminationWindowAsync(TestClient client)
        {
            await client.SendAsync(TalkToTheArchaeologist);
            await client.ExpectAsync(ArchaeologistsDialog, "the archaeologist's dialog");
            var asked = client.Mark();
            await client.SendAsync(AskForTheExaminationWindow);
            PacketAssert.AreEqual(new WopenPacket { Type = WindowType.ExamineRelics },
                await client.ExpectSinceAsync<WopenPacket>(asked, null, "the examination window"));
        }

        [TestMethod]
        public async Task TheArchaeologistsMenuOpensTheExaminationWindow()
        {
            await using var alice = await HoldingARelictAsync();

            await OpenTheExaminationWindowAsync(alice);
        }

        [TestMethod]
        public async Task ConfirmingTheFeeTradesTheRelictForWhatItHeld()
        {
            await using var alice = await HoldingARelictAsync();
            await OpenTheExaminationWindowAsync(alice);
            var confirmed = alice.Mark();

            await alice.SendAsync(ConfirmTheFee);

            await alice.ExpectSinceAsync<GoldPacket>(confirmed, p => p.Gold == Gold - WorldData.RelictFee, "the gold less the fee");
            PacketAssert.AreEqual(new RdiPacket { ItemVNum = WorldData.RelictPiece, Amount = 1 },
                await alice.ExpectSinceAsync<RdiPacket>(confirmed, null, "the piece announced"));
        }
    }
}
