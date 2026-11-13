//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class ChatTests
    {
        [TestMethod]
        public async Task AWhisperReachesThePlayerItNames()
        {
            var bobPlayer = await Harness.AddPlayerAsync("Bob", WorldData.Meadow, 20, 20);
            await using var bob = Harness.Client(bobPlayer);
            await bob.EnterWorldAsync(bobPlayer.Slot);
            await using var alice = await Harness.EnterAsync("Alice");
            await Task.Delay(Harness.WhoIsOnlineIsCachedFor);

            await alice.SendAsync($"/{bobPlayer.Name} see you at the portal");

            var heard = await bob.ExpectAsync("spk");
            StringAssert.StartsWith(heard, $"spk 1 {alice.CharacterId} ");
            StringAssert.Contains(heard, "see you at the portal");
        }

        [TestMethod]
        public async Task AWhisperToNobodySaysSo()
        {
            await using var alice = await Harness.EnterAsync("Alice");

            await alice.SendAsync("/Nobody are you there");

            // 2 is the client's "%s is not playing."
            await alice.ExpectAsync(p => p.StartsWith("infoi2 ", StringComparison.Ordinal) && p.EndsWith(" Nobody", StringComparison.Ordinal),
                "the not-playing notice");
        }

        [TestMethod]
        public async Task AMutedAccountsWordsReachNobody()
        {
            var player = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            await using (var context = Harness.Database.CreateContext())
            {
                var account = await Harness.StoredAsync(player.CharacterId);
                var now = SystemClock.Instance.GetCurrentInstant();
                context.Add(new PenaltyLog
                {
                    AccountId = account.AccountId, Penalty = PenaltyType.Muted, AdminName = "admin", Reason = "test",
                    DateStart = now, DateEnd = now.Plus(Duration.FromMinutes(30))
                });
                await context.SaveChangesAsync();
            }

            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);

            var said = bob.Mark();
            await alice.SendAsync("say hello");

            // 221 is the client's "<I am serving a penalty.>", 566 its "Remaining punishment: %d minutes".
            await bob.ExpectAsync(p => p == $"sayi 1 {alice.CharacterId} 1 221 0 0 0 0 0", "the penalty bubble");
            await alice.ExpectAsync(p => p == $"sayi 1 {alice.CharacterId} 11 566 4 30 0 0 0", "the minutes left");
            Assert.IsTrue(await bob.NoneSinceAsync(said, p => TestClient.HeaderOf(p) == "say", TimeSpan.FromSeconds(1)),
                "the words themselves are sent to nobody");
        }
    }
}
