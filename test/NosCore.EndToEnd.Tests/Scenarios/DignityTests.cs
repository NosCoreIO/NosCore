//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class DignityTests
    {
        private static Func<InPacket, bool> PreyOnTheMap =>
            packet => packet.VisualType == VisualType.Monster && packet.VNum == WorldData.PreyVNum.ToString(CultureInfo.InvariantCulture)
                && packet.VisualId == WorldData.Prey;

        [TestMethod]
        public async Task CompletingAQuestRestoresDignityToTheMaximumWithoutAnnouncingIt()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 30, 71, c => c.Dignity = -72);
            await Harness.GiveQuestAsync(player, WorldData.PreyQuest);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);

            await alice.ExpectSinceAsync(0, PreyOnTheMap, "the prey on the map");
            var (_, cast) = await Steps.KillAsync(alice, WorldData.Prey);

            await alice.ExpectSinceAsync<FdPacket>(cast, p => p.Dignity == 100, "dignity back at the maximum", TimeSpan.FromSeconds(10));
            Assert.IsTrue(await alice.NoneSinceAsync<SayiPacket>(cast, p => p.Message == Game18NConstString.DignityIncreased, TimeSpan.FromSeconds(1)));
        }
    }
}
