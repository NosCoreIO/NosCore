//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Chat;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Chat
{
    [TestClass]
    public class ClientSayPacketHandlerTests
    {
        private ClientSayPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new ClientSayPacketHandler();
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

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
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
