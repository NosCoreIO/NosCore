//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.Event;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventService;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class EventPacketHandlerTests
    {
        private TestWorld World;
        private EventPacketHandler Handler;
        private ClientSession Session;
        private Mock<IEventService> Events;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Events = World.Freeze<Mock<IEventService>>();
            Handler = World.Create<EventPacketHandler>();
        }

        [TestMethod]
        public async Task TheEventStartsAtOnce()
        {
            await new Spec("The event starts at once without a countdown")
                .Given(TheEventCanStart_, true)
                .WhenAsync(StartingTheEvent)
                .Then(TheEventShouldBeStartedWithoutCountdown)
                .And(NothingShouldBeSaid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEventAlreadyRunningIsReported()
        {
            await new Spec("An event already running is reported to the sender")
                .Given(TheEventCanStart_, false)
                .WhenAsync(StartingTheEvent)
                .Then(TheSenderShouldBeToldItIsRunning)
                .ExecuteAsync();
        }

        private void TheEventCanStart_(bool canStart) =>
            Events.Setup(x => x.TryStart(EventType.IceBreaker, false)).Returns(canStart);

        private Task StartingTheEvent() =>
            Handler.ExecuteAsync(new EventPacket { EventType = EventType.IceBreaker }, Session);

        private void TheEventShouldBeStartedWithoutCountdown() =>
            Events.Verify(x => x.TryStart(EventType.IceBreaker, false), Times.Once);

        private void NothingShouldBeSaid() => Assert.IsFalse(Session.LastPackets.OfType<SayPacket>().Any());

        private void TheSenderShouldBeToldItIsRunning() =>
            PacketAssert.AreEqual(new SayPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = World.GameLanguageLocalizer[LanguageKey.EVENT_ALREADY_RUNNING, Session.Account.Language],
            }, Session.LastPackets.OfType<SayPacket>().Single());
    }
}
