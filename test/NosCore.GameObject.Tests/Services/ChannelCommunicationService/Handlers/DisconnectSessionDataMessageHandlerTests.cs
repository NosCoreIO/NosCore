//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class DisconnectSessionDataMessageHandlerTests
    {
        private const string Account = "account";
        private const long SessionId = 42;

        private TestWorld World;
        private DisconnectSessionDataMessageChannelCommunicationMessageHandler Handler;
        private ClientSession Session;
        private int Disconnects;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Handler = World.Create<DisconnectSessionDataMessageChannelCommunicationMessageHandler>();
            World.SessionRegistry.Register(new SessionInfo
            {
                ChannelId = "closing-channel",
                SessionId = SessionId,
                Sender = Session,
                AccountName = Account,
                Disconnect = () =>
                {
                    Disconnects++;
                    return Task.CompletedTask;
                },
            });
        }

        [TestMethod]
        public async Task TheSessionHoldingTheAccountIsClosed()
        {
            await new Spec("The session holding the account is closed")
                .WhenAsync(Closing_Of_, SessionId, Account)
                .Then(TheSessionShouldBeClosed_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASessionOfAnotherAccountIsLeftOpen()
        {
            await new Spec("A session of another account is left open")
                .WhenAsync(Closing_Of_, SessionId, "someone-else")
                .Then(TheSessionShouldBeClosed_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASessionHandedOverToAnotherChannelIsLeftOpen()
        {
            await new Spec("A session handed over to another channel is left open")
                .GivenAsync(TheSessionIsHandedOver)
                .WhenAsync(Closing_Of_, SessionId, Account)
                .Then(TheSessionShouldBeClosed_Times, 0)
                .ExecuteAsync();
        }

        private Task TheSessionIsHandedOver() => Session.HandOverAsync();

        private Task Closing_Of_(long sessionId, string account) =>
            Handler.Handle(new DisconnectSessionData { SessionId = sessionId, AccountName = account });

        private void TheSessionShouldBeClosed_Times(int times)
        {
            Assert.AreEqual(times, Disconnects);
        }
    }
}
