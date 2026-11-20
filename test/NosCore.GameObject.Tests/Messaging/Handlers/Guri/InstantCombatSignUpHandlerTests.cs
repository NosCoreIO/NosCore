//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InstantCombatService;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class InstantCombatSignUpHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IInstantCombatService> _instantCombat;
        private InstantCombatSignUpHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _instantCombat = World.Freeze<Mock<IInstantCombatService>>();
            _handler = World.Create<InstantCombatSignUpHandler>();
        }

        [TestMethod]
        public async Task OpeningTheQuestionDoesNotSignUp()
        {
            await new Spec("Opening the Instant Combat question from its icon does not sign the player up")
                .WhenAsync(TheClientSends_, GuriPacketType.InstantCombatQuestionOpened)
                .Then(ThePlayerShouldBeSignedUp_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnsweringYesSignsUp()
        {
            await new Spec("Answering yes to the Instant Combat question signs the player up")
                .WhenAsync(TheClientSends_, GuriPacketType.InstantCombatQuestionOpened)
                .AndAsync(TheClientSends_, GuriPacketType.EventInWaiting)
                .Then(ThePlayerShouldBeSignedUp_Times, 1)
                .ExecuteAsync();
        }

        private Task TheClientSends_(GuriPacketType type) =>
            _handler.Handle(new GuriPacketReceivedEvent(_session, new GuriPacket { Type = type }));

        private void ThePlayerShouldBeSignedUp_Times(int times) =>
            _instantCombat.Verify(s => s.SignUp(_session), Times.Exactly(times));
    }
}
