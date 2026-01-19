//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class MailDataMessageHandlerTests
    {
        private MailDataMessageChannelCommunicationMessageHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<ISessionRegistry> SessionRegistry = null!;
        private Mock<IGameLanguageLocalizer> GameLanguageLocalizer = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            SessionRegistry = new Mock<ISessionRegistry>();
            GameLanguageLocalizer = new Mock<IGameLanguageLocalizer>();
            GameLanguageLocalizer.Setup(x => x[It.IsAny<LanguageKey>(), It.IsAny<RegionType>()])
                .Returns(new LocalizedString("key", "Item gifted: {0}"));
            Handler = new MailDataMessageChannelCommunicationMessageHandler(GameLanguageLocalizer.Object, SessionRegistry.Object);
        }

        [TestMethod]
        public async Task HandleShouldSendMailWhenCharacterFound()
        {
            await new Spec("Handle should send mail when character found")
                .Given(CharacterIsRegistered)
                .WhenAsync(HandlingMailData)
                .Then(ShouldSendMailPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleShouldDoNothingWhenCharacterNotFound()
        {
            await new Spec("Handle should do nothing when character not found")
                .Given(CharacterIsNotRegistered)
                .WhenAsync(HandlingMailData)
                .Then(ShouldNotSendAnyPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HandleWithItemShouldSendSayMessage()
        {
            await new Spec("Handle with item should send say message")
                .Given(CharacterIsRegistered)
                .WhenAsync(HandlingMailDataWithItem)
                .Then(ShouldSendSayPacket)
                .ExecuteAsync();
        }

        private void CharacterIsRegistered()
        {
            SessionRegistry.Setup(x => x.GetCharacter(It.IsAny<System.Func<ICharacterEntity, bool>>()))
                .Returns(Session.Character);
            Session.LastPackets.Clear();
        }

        private void CharacterIsNotRegistered()
        {
            SessionRegistry.Setup(x => x.GetCharacter(It.IsAny<System.Func<ICharacterEntity, bool>>()))
                .Returns((ICharacterEntity?)null);
            Session.LastPackets.Clear();
        }

        private async Task HandlingMailData()
        {
            await Handler.Handle(new MailData
            {
                ReceiverName = Session.Character.Name,
                MailId = 1,
                MailDto = new MailDto { Title = "Test Mail" },
                SenderName = "Sender"
            });
        }

        private async Task HandlingMailDataWithItem()
        {
            await Handler.Handle(new MailData
            {
                ReceiverName = Session.Character.Name,
                MailId = 1,
                MailDto = new MailDto { Title = "Test Mail with Item" },
                SenderName = "Sender",
                ItemInstance = new ItemInstanceDto
                {
                    Amount = 5,
                    ItemVNum = 1012
                }
            });
        }

        private void ShouldSendMailPacket()
        {
            Assert.IsTrue(Session.LastPackets.Count > 0);
        }

        private void ShouldNotSendAnyPacket()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void ShouldSendSayPacket()
        {
            var sayPacket = Session.LastPackets.OfType<SayPacket>().FirstOrDefault();
            Assert.IsNotNull(sayPacket);
        }
    }
}
