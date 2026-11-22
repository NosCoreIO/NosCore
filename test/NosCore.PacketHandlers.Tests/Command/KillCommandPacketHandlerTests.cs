//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class KillCommandPacketHandlerTests
    {
        private TestWorld World;
        private KillCommandPacketHandler Handler;
        private ClientSession Session;
        private Mock<IGameEventDispatcher> GameEvents;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            GameEvents = World.Freeze<Mock<IGameEventDispatcher>>();
            Handler = World.Create<KillCommandPacketHandler>();
        }

        [TestMethod]
        public async Task ALivingCharacterDiesByItsOwnHand()
        {
            await new Spec("A living character dies by its own hand")
                .Given(TheCharacterHasHp_, 100)
                .WhenAsync(Killing)
                .Then(TheCharacterShouldHaveNoHp)
                .And(TheDeathShouldBeRaised_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeadCharacterIsNotKilledAgain()
        {
            await new Spec("A dead character is not killed again")
                .Given(TheCharacterHasHp_, 0)
                .WhenAsync(Killing)
                .Then(TheDeathShouldBeRaised_, 0)
                .ExecuteAsync();
        }

        private void TheCharacterHasHp_(int hp) => Session.Character.Hp = hp;

        private Task Killing() => Handler.ExecuteAsync(new KillPacket(), Session);

        private void TheCharacterShouldHaveNoHp() => Assert.AreEqual(0, Session.Character.Hp);

        private void TheDeathShouldBeRaised_(int times)
        {
            var self = Session.Character.VisualId;
            GameEvents.Verify(x => x.RaiseAsync(It.Is<EntityDiedEvent>(e => IsSelfKill(e, self))), Times.Exactly(times));
        }

        private static bool IsSelfKill(EntityDiedEvent died, long self) =>
            died.Victim.VisualId == self && died.Killer?.VisualId == self;
    }
}
