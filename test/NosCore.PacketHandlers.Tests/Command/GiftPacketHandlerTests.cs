//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.RemoteCharacterService;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class GiftPacketHandlerTests
    {
        private GiftPacketHandler Handler;
        private ClientSession Session;
        private Mock<IRemoteCharacterService> RemoteCharacters;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            RemoteCharacters = new Mock<IRemoteCharacterService>();
            Handler = new GiftPacketHandler(RemoteCharacters.Object);
        }

        [TestMethod]
        public async Task SendingGiftToUnknownPlayerShouldShowError()
        {
            await new Spec("Sending gift to unknown player should show error")
                .WhenAsync(SendingGiftTo_, "UnknownPlayer123")
                .Then(ShouldReceiveUnknownCharacterMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SendingGiftToOnlinePlayerShouldSendIt()
        {
            await new Spec("Sending gift to online player forwards the item and confirms delivery")
                .Given(TargetPlayerIsOnline)
                .WhenAsync(SendingGiftTo_, "TargetPlayer")
                .Then(TheGiftShouldBeForwarded)
                .And(ShouldReceiveGiftDeliveredMessage)
                .ExecuteAsync();
        }

        private void TargetPlayerIsOnline() =>
            RemoteCharacters.Setup(x => x.SendGiftAsync(It.IsAny<ICharacterEntity>(), "TargetPlayer", It.IsAny<short>(),
                It.IsAny<short>(), It.IsAny<sbyte>(), It.IsAny<byte>())).ReturnsAsync(true);

        private Task SendingGiftTo_(string name) =>
            Handler.ExecuteAsync(new GiftPacket { CharacterName = name, VNum = 1012, Amount = 10, Rare = 5, Upgrade = 7 },
                Session);

        private void TheGiftShouldBeForwarded() =>
            RemoteCharacters.Verify(x => x.SendGiftAsync(It.IsAny<ICharacterEntity>(), "TargetPlayer", 1012, 10, 5, 7),
                Times.Once);

        private void ShouldReceiveUnknownCharacterMessage() =>
            Assert.AreEqual(Game18NConstString.UnknownCharacter, Session.LastPackets.OfType<InfoiPacket>().Single().Message);

        private void ShouldReceiveGiftDeliveredMessage() =>
            Assert.AreEqual(Game18NConstString.GiftDelivered, Session.LastPackets.OfType<SayiPacket>().Single().Message);
    }
}
