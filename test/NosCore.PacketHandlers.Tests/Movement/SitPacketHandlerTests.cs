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
using Microsoft.Extensions.Logging;
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
        private ClientSession OtherSession = null!;
        private readonly ILogger<SitPacketHandler> Logger = new Mock<ILogger<SitPacketHandler>>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            OtherSession = await TestHelpers.Instance.GenerateSessionAsync();
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

        [TestMethod]
        public async Task SittingSelfTogglesIsSittingOn()
        {
            await new Spec("Sitting self flips IsSitting from false to true")
                .Given(CharacterIsOnMap)
                .And(CharacterIsStanding)
                .WhenAsync(SittingWithOwnCharacter)
                .Then(IsSittingShouldBe_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SittingSelfTwiceTogglesIsSittingBackOff()
        {
            await new Spec("Calling sit twice stands the character back up")
                .Given(CharacterIsOnMap)
                .And(CharacterIsStanding)
                .WhenAsync(SittingWithOwnCharacter)
                .AndAsync(SittingWithOwnCharacter)
                .Then(IsSittingShouldBe_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AttemptingToSitAnotherPlayerDoesNotFlipTheirState()
        {
            await new Spec("A sit packet targeting a different player's VisualId does not flip their sit state")
                .Given(CharacterIsOnMap)
                .And(CharacterIsStanding)
                .And(OtherPlayerIsOnSameMap)
                .And(OtherPlayerIsStanding)
                .WhenAsync(SittingOtherPlayer)
                .Then(OtherPlayerIsSittingShouldBe_, false)
                .And(IsSittingShouldBe_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NonExistentPlayerIdIsSilentlyIgnored()
        {
            await new Spec("Sit packet targeting a VisualId not in the session registry is ignored")
                .Given(CharacterIsOnMap)
                .And(CharacterIsStanding)
                .WhenAsync(SittingOnUnknownVisualId)
                .Then(IsSittingShouldBe_, false)
                .ExecuteAsync();
        }

        private void CharacterIsStanding()
        {
            Session.Character.IsSitting = false;
        }

        private void OtherPlayerIsStanding()
        {
            OtherSession.Character.IsSitting = false;
        }

        private void OtherPlayerIsOnSameMap()
        {
            OtherSession.Character.MapInstance = Session.Character.MapInstance;
        }

        private async Task SittingOtherPlayer()
        {
            await Handler.ExecuteAsync(new SitPacket
            {
                Users = new List<SitSubPacket?>
                {
                    new SitSubPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = OtherSession.Character.VisualId
                    }
                }
            }, Session);
        }

        private async Task SittingOnUnknownVisualId()
        {
            await Handler.ExecuteAsync(new SitPacket
            {
                Users = new List<SitSubPacket?>
                {
                    new SitSubPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = int.MaxValue
                    }
                }
            }, Session);
        }

        private void IsSittingShouldBe_(bool expected) =>
            Assert.AreEqual(expected, Session.Character.IsSitting);

        private void OtherPlayerIsSittingShouldBe_(bool expected) =>
            Assert.AreEqual(expected, OtherSession.Character.IsSitting);

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
