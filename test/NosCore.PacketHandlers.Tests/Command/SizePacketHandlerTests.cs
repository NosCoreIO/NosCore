//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class SizePacketHandlerTests
    {
        private SizePacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<ILogger> Logger = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> LogLanguage = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Logger = new Mock<ILogger>();
            LogLanguage = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            Handler = new SizePacketHandler(Logger.Object, LogLanguage.Object);
        }

        [TestMethod]
        public async Task SizeOnPlayerShouldChangePlayerSize()
        {
            await new Spec("Size on player should change player size")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingSizeCommandOnPlayer_, 20)
                .Then(PlayerSizeShouldBe_, 20)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SizeOnUnknownVisualTypeShouldLogError()
        {
            await new Spec("Size on unknown visual type should log error")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingSizeCommandWithUnknownVisualType)
                .Then(ShouldLogError)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SizeOnNonExistentEntityShouldLogError()
        {
            await new Spec("Size on non existent entity should log error")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingSizeCommandOnNonExistentNpc)
                .Then(ShouldLogError)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            Session.LastPackets.Clear();
        }

        private async Task ExecutingSizeCommandOnPlayer_(int newSize)
        {
            await Handler.ExecuteAsync(new SizePacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.CharacterId,
                Size = (byte)newSize
            }, Session);
        }

        private async Task ExecutingSizeCommandWithUnknownVisualType()
        {
            await Handler.ExecuteAsync(new SizePacket
            {
                VisualType = unchecked((VisualType)99),
                VisualId = 1,
                Size = 20
            }, Session);
        }

        private async Task ExecutingSizeCommandOnNonExistentNpc()
        {
            await Handler.ExecuteAsync(new SizePacket
            {
                VisualType = VisualType.Npc,
                VisualId = 999999,
                Size = 20
            }, Session);
        }

        private void PlayerSizeShouldBe_(int expectedSize)
        {
            Assert.AreEqual((byte)expectedSize, Session.Character.Size);
        }

        private void ShouldLogError()
        {
            Logger.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<VisualType>()), Times.AtLeastOnce);
        }
    }
}
