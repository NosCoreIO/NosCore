//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class LoginToWorldTests
    {
        [TestMethod]
        public async Task TheLoginServerHandsOverToTheWorld()
        {
            await using var client = Harness.Client(await Harness.AddPlayerAsync("Alice"));

            var reply = await client.LoginAsync();

            Assert.IsNotNull(reply.World, reply.Raw);
            Assert.AreNotEqual(0, reply.Session);
        }

        [TestMethod]
        public async Task AWrongPasswordIsRefused()
        {
            await using var client = Harness.Client(await Harness.AddPlayerAsync("Alice"), "wrong");

            var reply = await client.LoginAsync();

            Assert.IsNull(reply.World);
            Assert.AreEqual("failc 5", reply.Refusal);
        }

        [TestMethod]
        public async Task TheCharacterListShowsTheAccountsCharacter()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);

            await client.ConnectAsync();

            var listed = client.Received.Single(p => TestClient.HeaderOf(p) == "clist").Split(' ');
            Assert.AreEqual(player.Slot.ToString(), listed[1]);
            Assert.AreEqual(player.Name, listed[2]);
        }

        [TestMethod]
        public async Task ACharacterEntersTheWorldWhereItWasSaved()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Town, 85, 120);
            await using var client = Harness.Client(player);

            var entry = await client.EnterWorldAsync(player.Slot);

            Assert.AreEqual(player.CharacterId, client.CharacterId);
            Assert.AreEqual($"at {player.CharacterId} {WorldData.Town} 85 120", string.Join(' ', At(entry).Take(5)));
            Assert.IsTrue(entry.Any(p => TestClient.HeaderOf(p) == "stat"), "the character's HP and MP are sent");
        }

        [TestMethod]
        public async Task TheSameAccountCannotLogInTwice()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var first = Harness.Client(player);
            await first.EnterWorldAsync(player.Slot);
            await using var second = Harness.Client(player);

            var reply = await second.LoginAsync();

            Assert.AreEqual("failc 4", reply.Refusal);
        }

        private static string[] At(System.Collections.Generic.IEnumerable<string> packets) =>
            packets.Single(p => TestClient.HeaderOf(p) == "at").Split(' ');
    }
}
