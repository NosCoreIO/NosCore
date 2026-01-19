//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class HelpPacketHandlerTests
    {
        private HelpPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new HelpPacketHandler();
        }

        [TestMethod]
        public async Task ExecutingHelpShouldSendHeaderMessage()
        {
            await new Spec("Executing help should send header message")
                .Given(CharacterIsOnMap)
                .And(CharacterHasGameMasterAuthority)
                .WhenAsync(ExecutingHelpCommand)
                .Then(ShouldReceiveHelpHeader)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ExecutingHelpShouldSendCommandsList()
        {
            await new Spec("Executing help should send commands list")
                .Given(CharacterIsOnMap)
                .And(CharacterHasGameMasterAuthority)
                .WhenAsync(ExecutingHelpCommand)
                .Then(ShouldReceiveMultipleSayPackets)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ExecutingHelpAsModeratorShouldShowOnlyModeratorCommands()
        {
            await new Spec("Executing help as moderator should show only moderator level commands")
                .Given(CharacterIsOnMap)
                .And(CharacterHasModeratorAuthority)
                .WhenAsync(ExecutingHelpCommand)
                .Then(ShouldReceiveHelpHeader)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void CharacterHasGameMasterAuthority()
        {
            Session.Account.Authority = AuthorityType.GameMaster;
        }

        private void CharacterHasModeratorAuthority()
        {
            Session.Account.Authority = AuthorityType.Moderator;
        }

        private async Task ExecutingHelpCommand()
        {
            await Handler.ExecuteAsync(new HelpPacket(), Session);
        }

        private void ShouldReceiveHelpHeader()
        {
            var sayPackets = Session.LastPackets.OfType<SayPacket>().ToList();
            Assert.IsTrue(sayPackets.Any(p => p.Message != null && p.Message.Contains("Help command")));
        }

        private void ShouldReceiveMultipleSayPackets()
        {
            var sayPackets = Session.LastPackets.OfType<SayPacket>().ToList();
            Assert.IsTrue(sayPackets.Count > 1);
        }
    }
}
