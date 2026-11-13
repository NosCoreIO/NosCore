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

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class CharacterScreenTests
    {
        [TestMethod]
        public async Task ANewCharacterIsListedAndCanEnterTheWorld()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);
            await client.ConnectAsync();
            var name = "New" + player.Name;

            await client.SendAsync($"Char_NEW {name} 1 0 0 0");

            await client.ExpectAsync("clist_start");
            await client.ExpectAsync(p => p.StartsWith($"clist 1 {name} ", StringComparison.Ordinal), "the new character in slot 1");
            await client.ExpectAsync("clist_end");

            var entry = await client.EnterWorldAsync(1);
            Assert.IsTrue(entry.Any(p => p.StartsWith($"at {client.CharacterId} {WorldData.Town} ", StringComparison.Ordinal)),
                "a new character starts in the town");
        }

        [TestMethod]
        public async Task ANameThatIsTakenIsRefused()
        {
            var taken = await Harness.AddPlayerAsync("Bob");
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);
            await client.ConnectAsync();
            var created = client.Mark();

            await client.SendAsync($"Char_NEW {taken.Name} 1 0 0 0");

            await client.ExpectAsync("infoi");
            Assert.IsTrue(await client.NoneSinceAsync(created, p => p.StartsWith("clist 1 ", StringComparison.Ordinal), TimeSpan.FromSeconds(1)));
        }

        [TestMethod]
        public async Task ADeletedCharacterLeavesTheList()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);
            await client.ConnectAsync();
            var deleted = client.Mark();

            await client.SendAsync($"Char_DEL {player.Slot} {WorldData.Password}");

            await client.ExpectAsync("clist_start");
            await client.ExpectAsync("clist_end");
            Assert.IsFalse(client.Since(deleted).Any(p => p.StartsWith("clist ", StringComparison.Ordinal)), "the slot is empty");
        }
    }
}
