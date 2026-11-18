//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class SoundFlowerTests
    {
        private static async Task<TestClient> HoldingAFlowerAsync(byte level)
        {
            var player = await Harness.AddPlayerAsync("Alice", configure: c => c.Level = level);
            await Harness.GiveAsync(player, WorldData.SoundFlower, 1, pocket: NoscorePocketType.Main);
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

        private static Task UseAsync(TestClient client) => client.SendAsync(new UseItemPacket
        {
            VisualType = VisualType.Player, VisualId = client.CharacterId, Type = PocketType.Main, Slot = 0, Mode = 0, Parameter = 0
        });

        private static bool FlowerGone(IvnPacket packet) =>
            packet.Type == PocketType.Main && packet.IvnSubPackets?.Single() is { } item && item.Slot == 0 && item.VNum is 0 or -1;

        [TestMethod]
        public async Task UsingASoundFlowerStartsTheFlowerQuestAndUsesItUp()
        {
            await using var alice = await HoldingAFlowerAsync(WorldData.FlowerQuestLevel);

            var used = alice.Mark();
            await UseAsync(alice);

            await alice.ExpectSinceAsync<QstlistPacket>(used,
                p => p.QuestSubPackets.Any(q => q.QuestId == WorldData.FlowerQuest && q.QuestType == QuestType.FlowerQuest),
                "the flower quest in the quest window");
            await alice.ExpectSinceAsync<IvnPacket>(used, FlowerGone, "the flower gone from the bag");
        }

        [TestMethod]
        public async Task ACharacterBelowTheQuestLevelKeepsTheFlower()
        {
            await using var alice = await HoldingAFlowerAsync((byte)(WorldData.FlowerQuestLevel - 1));

            var used = alice.Mark();
            await UseAsync(alice);

            PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.LevelTooLow },
                await alice.ExpectSinceAsync<MsgiPacket>(used, p => p.Message == Game18NConstString.LevelTooLow, "the level refusal"));
            Assert.IsTrue(await alice.NoneSinceAsync<IvnPacket>(used, FlowerGone, TimeSpan.FromSeconds(1)), "the flower stays");
        }
    }
}
