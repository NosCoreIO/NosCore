//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Movement;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
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
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            OtherSession = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new SitPacketHandler(
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry,
                TestHelpers.Instance.Clock);
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
        public async Task StandingUpThrowsAwayTheMealAtOnce()
        {
            await new Spec("Standing up empties the food schedule, so sitting back down before the next tick keeps nothing")
                .Given(CharacterIsOnMap)
                .And(CharacterIsSittingWithFoodDigesting)
                .WhenAsync(SittingWithOwnCharacter)
                .Then(IsSittingShouldBe_, false)
                .And(NoFoodShouldBeDigesting)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SittingDownKeepsTheMeal()
        {
            await new Spec("Only standing up throws food away")
                .Given(CharacterIsOnMap)
                .And(CharacterIsStandingWithFoodDigesting)
                .WhenAsync(SittingWithOwnCharacter)
                .Then(IsSittingShouldBe_, true)
                .And(FoodShouldStillBeDigesting)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SittingDownJustAfterAttackingIsIgnored()
        {
            await new Spec("Within three seconds of attacking the sit request gets no answer")
                .Given(CharacterIsOnMap)
                .And(CharacterIsStanding)
                .And(CharacterAttacked_MillisecondsAgo, 2900)
                .WhenAsync(SittingWithOwnCharacter)
                .Then(IsSittingShouldBe_, false)
                .And(TheMapShouldSeeNoRestPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SittingDownThreeSecondsAfterAttackingWorks()
        {
            await new Spec("The lock is over three seconds after the last attack")
                .Given(CharacterIsOnMap)
                .And(CharacterIsStanding)
                .And(CharacterAttacked_MillisecondsAgo, 3000)
                .WhenAsync(SittingWithOwnCharacter)
                .Then(IsSittingShouldBe_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StandingUpIsNeverBlocked()
        {
            await new Spec("Only sitting down waits for the lock")
                .Given(CharacterIsOnMap)
                .And(CharacterIsSittingWithFoodDigesting)
                .And(CharacterAttacked_MillisecondsAgo, 0)
                .WhenAsync(SittingWithOwnCharacter)
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

        private void CharacterIsSittingWithFoodDigesting()
        {
            Session.Character.IsSitting = true;
            Session.Character.Digestion.Food.Add(140, 0);
        }

        private void CharacterIsStandingWithFoodDigesting()
        {
            Session.Character.IsSitting = false;
            Session.Character.Digestion.Food.Add(140, 0);
        }

        private void CharacterAttacked_MillisecondsAgo(int milliseconds) =>
            Session.Character.LastAttack =
                TestHelpers.Instance.Clock.GetCurrentInstant() - Duration.FromMilliseconds(milliseconds);

        private void TheMapShouldSeeNoRestPacket() =>
            Assert.IsFalse(Session.Character.MapInstance.LastPackets.OfType<RestPacket>().Any());

        private void NoFoodShouldBeDigesting() => Assert.IsTrue(Session.Character.Digestion.Food.IsEmpty);

        private void FoodShouldStillBeDigesting() => Assert.IsFalse(Session.Character.Digestion.Food.IsEmpty);

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
            Session.Character.MapInstance.LastPackets.Clear();
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
