//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.PenaltyService;
using NosCore.PacketHandlers.Chat;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Chat
{
    [TestClass]
    public class ClientSayPacketHandlerTests
    {
        private TestWorld World;
        private ClientSayPacketHandler Handler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            World.Inject<IPenaltyService>(World.Create<PenaltyService>());
            Handler = World.Create<ClientSayPacketHandler>();
        }

        [TestMethod]
        public async Task SayMessageShouldExecuteWithoutError()
        {
            await new Spec("Say message should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(SendingSayMessage)
                .Then(HandlerShouldCompleteSuccessfully)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SayMessageShouldNotBeSentBackToSender()
        {
            await new Spec("Say message should not be sent back to sender")
                .Given(CharacterIsOnMap)
                .WhenAsync(SendingSayMessage)
                .Then(SenderShouldNotReceiveOwnMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EmptyMessageShouldBeHandled()
        {
            await new Spec("Empty message should be handled")
                .Given(CharacterIsOnMap)
                .WhenAsync(SendingEmptyMessage)
                .Then(HandlerShouldCompleteSuccessfully)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMutedSpeakersWordsReachNobody()
        {
            await new Spec("A muted speaker's words reach nobody")
                .Given(CharacterIsOnMap)
                .AndAsync(TheAccountIsMutedFor_Minutes, 90)
                .WhenAsync(SendingSayMessage)
                .Then(TheMapSeesThePenaltyBubbleInsteadOfTheWords)
                .And(TheSpeakerIsToldTheMinutesLeft, 90L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheMinutesLeftAreRoundedUp()
        {
            await new Spec("The minutes left are rounded up")
                .Given(CharacterIsOnMap)
                .AndAsync(TheAccountIsMutedFor_Minutes, 90)
                .And(Time_Passes, Duration.FromSeconds(30))
                .WhenAsync(SendingSayMessage)
                .Then(TheSpeakerIsToldTheMinutesLeft, 90L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMuteThatHasEndedLetsTheWordsThrough()
        {
            await new Spec("A mute that has ended lets the words through")
                .Given(CharacterIsOnMap)
                .AndAsync(TheAccountIsMutedFor_Minutes, 90)
                .And(Time_Passes, Duration.FromMinutes(90))
                .WhenAsync(SendingSayMessage)
                .Then(TheMapHearsTheWords)
                .ExecuteAsync();
        }

        private Task TheAccountIsMutedFor_Minutes(int minutes)
        {
            var now = World.Clock.GetCurrentInstant();
            return World.PenaltyLogDao.TryInsertOrUpdateAsync(new PenaltyLogDto
            {
                AccountId = Session.Account.AccountId,
                Penalty = PenaltyType.Muted,
                DateStart = now,
                DateEnd = now.Plus(Duration.FromMinutes(minutes)),
                AdminName = "admin",
                Reason = "test",
            });
        }

        private void Time_Passes(Duration time) => World.Clock.Advance(time);

        private void TheMapSeesThePenaltyBubbleInsteadOfTheWords()
        {
            var map = Session.Character.MapInstance;
            Assert.IsFalse(map.LastPackets.OfType<SayPacket>().Any());
            var bubble = map.LastPackets.OfType<SayiPacket>().Single();
            Assert.AreEqual(Game18NConstString.IAmUnderPenalty, bubble.Message);
            Assert.AreEqual(SayColorType.BubbleOnly, bubble.Type);
            Assert.AreEqual(Session.Character.VisualId, bubble.VisualId);
        }

        private void TheSpeakerIsToldTheMinutesLeft(long minutes)
        {
            var told = Session.LastPackets.OfType<SayiPacket>().Single(p => p.Message == Game18NConstString.RemainingPunishment);
            Assert.AreEqual(minutes, told.Game18NArguments[0]);
        }

        private void TheMapHearsTheWords()
        {
            var map = Session.Character.MapInstance;
            Assert.AreEqual("Hello everyone!", map.LastPackets.OfType<SayPacket>().Single().Message);
            Assert.IsFalse(map.LastPackets.OfType<SayiPacket>().Any());
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private async Task SendingSayMessage()
        {
            await Handler.ExecuteAsync(new ClientSayPacket
            {
                Message = "Hello everyone!"
            }, Session);
        }

        private async Task SendingEmptyMessage()
        {
            await Handler.ExecuteAsync(new ClientSayPacket
            {
                Message = ""
            }, Session);
        }

        private void HandlerShouldCompleteSuccessfully()
        {
            // If we reached here, the handler completed without throwing
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void SenderShouldNotReceiveOwnMessage()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
