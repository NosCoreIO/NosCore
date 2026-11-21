//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Packets.ServerPackets.Map;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Tests.Shared;

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

            Assert.IsNotNull(reply.World, reply.ToString());
            Assert.AreNotEqual(0, reply.Accepted?.SessionId);
        }

        [TestMethod]
        public async Task AWrongPasswordIsRefused()
        {
            await using var client = Harness.Client(await Harness.AddPlayerAsync("Alice"), "wrong");

            var reply = await client.LoginAsync();

            Assert.IsNull(reply.World);
            PacketAssert.AreEqual(new FailcPacket { Type = LoginFailType.AccountOrPasswordWrong }, reply.Refusal);
        }

        [TestMethod]
        public async Task TheCharacterListShowsTheAccountsCharacter()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);

            await client.ConnectAsync();

            var listed = client.Received.OfType<ClistPacket>().Single();
            Assert.AreEqual(player.Slot, listed.Slot);
            Assert.AreEqual(player.Name, listed.Name);
        }

        [TestMethod]
        public async Task ACharacterEntersTheWorldWhereItWasSaved()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Town, 85, 120);
            await using var client = Harness.Client(player);

            var entry = await client.EnterWorldAsync(player.Slot);

            Assert.AreEqual(player.CharacterId, client.CharacterId);
            var at = entry.OfType<AtPacket>().Single();
            Assert.AreEqual((player.CharacterId, WorldData.Town, (short)85, (short)120), (at.CharacterId, at.MapId, at.PositionX, at.PositionY));
            Assert.IsTrue(entry.OfType<StatPacket>().Any(), "the character's HP and MP are sent");
        }

        [TestMethod]
        public async Task EnteringTheWorldLeavesNoMapBeforeTheFirstCInfo()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);

            var entry = (await client.EnterWorldAsync(player.Slot)).ToList();

            var info = entry.FindIndex(p => p is CInfoPacket);
            Assert.IsFalse(entry.Take(info).Any(p => p is CMapPacket or MapOutPacket), "no map is left on entering the game");
            Assert.AreEqual(-1, entry.OfType<CInfoPacket>().First().Unknown3);
        }

        [TestMethod]
        public async Task EnteringTheWorldPinsTheNoticeBoardInsteadOfChatLines()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var client = Harness.Client(player);

            var entry = await client.EnterWorldAsync(player.Slot);

            var board = entry.OfType<BnPacket>().ToList();
            CollectionAssert.AreEqual(new[] { "Powered by NosCore", "GitHub: https://github.com/NosCoreIO/NosCore/" },
                board.Select(p => p.Message).ToList());
            CollectionAssert.AreEqual(new byte[] { 0, 1 }, board.Select(p => p.BnNumber).ToList());
            Assert.IsFalse(entry.OfType<SayPacket>().Any(), "the notice board replaces the chat lines");
        }

        [TestMethod]
        public async Task TheSameAccountCannotLogInTwice()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await using var first = Harness.Client(player);
            await first.EnterWorldAsync(player.Slot);
            await using var second = Harness.Client(player);

            var reply = await second.LoginAsync();

            PacketAssert.AreEqual(new FailcPacket { Type = LoginFailType.AlreadyConnected }, reply.Refusal);
        }
    }
}
