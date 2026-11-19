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
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
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
        // The flower is put on the ground from the bag, the one way a scenario can lay a drop at its feet.
        private static async Task<(TestClient Client, DropPacket Flower)> AFlowerAtTheFeetOfAsync(byte level)
        {
            var player = await Harness.AddPlayerAsync("Alice", configure: c => c.Level = level);
            await Harness.GiveAsync(player, WorldData.WildSoundFlower, 1, pocket: NoscorePocketType.Main);
            var client = Harness.Client(player);
            try
            {
                await client.EnterWorldAsync(player.Slot);
                await client.SendAsync(new PutPacket { PocketType = PocketType.Main, Slot = 0, Amount = 1 });
                var flower = await client.ExpectAsync<DropPacket>(p => p.VNum == WorldData.WildSoundFlower, "the flower on the ground");
                return (client, flower);
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }
        }

        private static Task PickUpAsync(TestClient client, DropPacket flower) => client.SendAsync(new GetPacket
        {
            PickerType = VisualType.Player, PickerId = client.CharacterId, VisualId = flower.VisualId
        });

        private static Func<ServerGetPacket, bool> PickedUp(TestClient client, DropPacket flower) =>
            p => p.VisualType == VisualType.Player && p.VisualId == client.CharacterId && p.ItemId == flower.VisualId;

        private static bool FlowerInTheBag(IvnPacket packet) =>
            packet.IvnSubPackets?.Any(item => item.VNum == WorldData.WildSoundFlower) == true;

        [TestMethod]
        public async Task PickingUpASoundFlowerStartsTheFlowerQuestWithoutFillingTheBag()
        {
            var (alice, flower) = await AFlowerAtTheFeetOfAsync(WorldData.FlowerQuestLevel);
            await using var _ = alice;

            var picked = alice.Mark();
            await PickUpAsync(alice, flower);

            await alice.ExpectSinceAsync<QstlistPacket>(picked,
                p => p.QuestSubPackets.Any(q => q.QuestId == WorldData.FlowerQuest && q.QuestType == QuestType.FlowerQuest),
                "the flower quest in the quest window");
            await alice.ExpectSinceAsync(picked, PickedUp(alice, flower), "the flower gone from the ground");
            Assert.IsTrue(await alice.NoneSinceAsync<IvnPacket>(picked, FlowerInTheBag, TimeSpan.FromSeconds(1)), "the bag is unchanged");
        }

        [TestMethod]
        public async Task ACharacterBelowTheQuestLevelLeavesTheFlowerOnTheGround()
        {
            var (alice, flower) = await AFlowerAtTheFeetOfAsync((byte)(WorldData.FlowerQuestLevel - 1));
            await using var _ = alice;

            var picked = alice.Mark();
            await PickUpAsync(alice, flower);

            PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.LevelTooLow },
                await alice.ExpectSinceAsync<MsgiPacket>(picked, p => p.Message == Game18NConstString.LevelTooLow, "the level refusal"));
            Assert.IsTrue(await alice.NoneSinceAsync(picked, PickedUp(alice, flower), TimeSpan.FromSeconds(1)), "the flower stays on the ground");
            Assert.IsFalse(alice.Since<IvnPacket>(picked).Any(FlowerInTheBag), "the bag is unchanged");
        }
    }
}
