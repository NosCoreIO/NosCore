//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class MaintenanceTests
    {
        // Only the second channel goes down, so the scenarios after this one still have a channel to enter.
        private const int Second = 1;

        private static async Task<Player> GameMasterAsync()
        {
            var player = await Harness.AddPlayerAsync("Gm");
            await using var context = Harness.Database.CreateContext();
            var account = await context.Set<Account>().SingleAsync(a => a.Name == player.Account);
            account.Authority = AuthorityType.GameMaster;
            await context.SaveChangesAsync();
            return player;
        }

        [TestMethod]
        public async Task AScheduledMaintenanceWarnsThePlayersAndThenDisconnectsThem()
        {
            var master = await GameMasterAsync();
            await using var gm = Harness.Client(master, channel: Second);
            await gm.EnterWorldAsync(master.Slot);
            var player = await Harness.AddPlayerAsync("Alice");
            await using var alice = Harness.Client(player, channel: Second);
            await alice.EnterWorldAsync(player.Slot);

            try
            {
                await gm.SendAsync(new SetMaintenancePacket { IsGlobal = false, MaintenanceMode = true, DelayInSeconds = 12 });

#pragma warning disable CS0618 // The warning has no client text, so it goes out as msg.
                await alice.ExpectAsync<MsgPacket>(p => p is
                {
                    Type: MessageType.Shout,
                    Message: "The servers will be shut down in 1 minute(s) for maintenance.",
                }, "the shutdown warning");
#pragma warning restore CS0618
                var countdown = await alice.ExpectAsync<MsgiPacket>(p => p.Message == Game18NConstString.ServerWillBeDisonnected,
                    "the countdown");
                Assert.AreEqual(4, countdown.ArgumentType);
                Assert.AreEqual(10L, Convert.ToInt64(countdown.Game18NArguments[0], CultureInfo.InvariantCulture), "the seconds left");
                await alice.ExpectAsync<MsgiPacket>(p => p.Message == Game18NConstString.ThanksForParticipating, "the last line");

                Assert.IsTrue(await alice.ClosedWithinAsync(TimeSpan.FromSeconds(10)), "the player is disconnected");
                Assert.IsFalse(await gm.ClosedWithinAsync(TimeSpan.FromSeconds(2)), "the game master stays");
            }
            finally
            {
                await gm.SendAsync(new SetMaintenancePacket { IsGlobal = false, MaintenanceMode = false });
            }

            await using var back = await Harness.ComeBackAsync(player, Second);
        }
    }
}
