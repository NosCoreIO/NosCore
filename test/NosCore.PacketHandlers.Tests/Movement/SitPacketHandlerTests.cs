//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Movement;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Movement
{
    [TestClass]
    public class SitPacketHandlerTests
    {
        private SitPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new SitPacketHandler(
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task SitWithOwnCharacterShouldExecute()
        {
            await new Spec("Sit with own character should execute")
                .Given(CharacterIsOnMap)
                .WhenAsync(SittingWithOwnCharacter)
                .Then(HandlerShouldComplete)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SitWithUnknownVisualTypeShouldBeIgnored()
        {
            await new Spec("Sit with unknown visual type should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(SittingWithUnknownVisualType)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task SittingWithOwnCharacter()
        {
            await Handler.ExecuteAsync(new SitPacket
            {
                Users = new List<SitSubPacket?>
                {
                    new SitSubPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = Session.Character.VisualId
                    }
                }
            }, Session);
        }

        private async Task SittingWithUnknownVisualType()
        {
            await Handler.ExecuteAsync(new SitPacket
            {
                Users = new List<SitSubPacket?>
                {
                    new SitSubPacket
                    {
                        VisualType = (VisualType)99,
                        VisualId = Session.Character.VisualId
                    }
                }
            }, Session);
        }

        private void HandlerShouldComplete()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
