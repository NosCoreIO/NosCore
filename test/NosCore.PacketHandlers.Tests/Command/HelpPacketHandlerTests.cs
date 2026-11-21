//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
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
        private TestWorld World;
        private HelpPacketHandler Handler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Handler = World.Create<HelpPacketHandler>();
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
            Session.Character.MapInstance = World.GetBaseMap(1);
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
            PacketAssert.AreEqual(new SayPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.CharacterId,
                Type = SayColorType.Red,
                Message = World.GameLanguageLocalizer[LanguageKey.HELP_COMMAND_HEADER, Session.Account.Language]
            }, Session.LastPackets.OfType<SayPacket>().First());
        }

        private void ShouldReceiveMultipleSayPackets()
        {
            var sayPackets = Session.LastPackets.OfType<SayPacket>().ToList();
            Assert.IsTrue(sayPackets.Count > 1);
        }
    }
}
