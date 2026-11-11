//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.RemoteCharacterService
{
    [TestClass]
    public class RemoteCharacterServiceTests
    {
        private const string TargetName = "TargetPlayer";
        private const long TargetId = 12345;

        private GameObject.Services.RemoteCharacterService.RemoteCharacterService Service;
        private ClientSession Session;
        private Mock<IPubSubHub> PubSubHub;
        private Mock<IMailHub> MailHub;
        private bool Found;
        private MailRequest SentMail;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            PubSubHub = new Mock<IPubSubHub>();
            MailHub = new Mock<IMailHub>();
            PubSubHub.Setup(x => x.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            MailHub.Setup(x => x.SendMailAsync(It.IsAny<MailRequest>()))
                .Callback<MailRequest>(request => SentMail = request)
                .ReturnsAsync(true);
            Service = new GameObject.Services.RemoteCharacterService.RemoteCharacterService(PubSubHub.Object,
                MailHub.Object, TestHelpers.Instance.Clock);
        }

        [TestMethod]
        public async Task AStatUpdateIsSentToAConnectedCharacter()
        {
            await new Spec("A stat update for a connected character is published with its name, action and value")
                .Given(TheTargetIsConnected)
                .WhenAsync(UpdatingTheTargetsLevelTo_, 99L)
                .Then(TheCharacterShouldBeFound_, true)
                .And(AStatUpdateShouldBePublishedWithLevel_, 99L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AStatUpdateForAnUnknownCharacterIsNotSent()
        {
            await new Spec("A stat update for a character nobody is connected as is not published")
                .WhenAsync(UpdatingTheTargetsLevelTo_, 99L)
                .Then(TheCharacterShouldBeFound_, false)
                .And(NothingShouldBePublished)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KickingAConnectedCharacterUnsubscribesThem()
        {
            await new Spec("Kicking a connected character unsubscribes them from the master server")
                .Given(TheTargetIsConnected)
                .WhenAsync(KickingTheTarget)
                .Then(TheCharacterShouldBeFound_, true)
                .And(TheTargetShouldBeUnsubscribed_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KickingAnUnknownCharacterDoesNothing()
        {
            await new Spec("Kicking a character nobody is connected as unsubscribes nobody")
                .WhenAsync(KickingTheTarget)
                .Then(TheCharacterShouldBeFound_, false)
                .And(TheTargetShouldBeUnsubscribed_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheGiftMailCarriesTheItemAndTheSender()
        {
            await new Spec("A gift to a connected character is mailed with the item and the sender")
                .Given(TheTargetIsConnected)
                .WhenAsync(GiftingTheTarget)
                .Then(TheCharacterShouldBeFound_, true)
                .And(TheMailShouldCarryTheItemAndTheSender)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGiftToAnUnknownCharacterIsNotMailed()
        {
            await new Spec("A gift to a character nobody is connected as is not mailed")
                .WhenAsync(GiftingTheTarget)
                .Then(TheCharacterShouldBeFound_, false)
                .And(NoMailShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KickingWithoutANameIgnoresSubscribersWithoutACharacter()
        {
            await new Spec("Kicking with no name does not match a subscriber that has no character")
                .Given(ASubscriberWithoutACharacterIsConnected)
                .WhenAsync(KickingNobodyByName)
                .Then(TheCharacterShouldBeFound_, false)
                .And(TheTargetShouldBeUnsubscribed_, 0)
                .ExecuteAsync();
        }

        private void ASubscriberWithoutACharacterIsConnected() =>
            PubSubHub.Setup(x => x.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber> { new() });

        private async Task KickingNobodyByName() => Found = await Service.KickAsync(null);

        private void TheTargetIsConnected() =>
            PubSubHub.Setup(x => x.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>
            {
                new() { ConnectedCharacter = new Character { Id = TargetId, Name = TargetName } }
            });

        private async Task UpdatingTheTargetsLevelTo_(long level) =>
            Found = await Service.UpdateStatAsync(TargetName, UpdateStatActionType.UpdateLevel, level);

        private async Task KickingTheTarget() => Found = await Service.KickAsync(TargetName);

        private async Task GiftingTheTarget() =>
            Found = await Service.SendGiftAsync(Session.Character, TargetName, 1, 1, 5, 7);

        private void TheCharacterShouldBeFound_(bool found) => Assert.AreEqual(found, Found);

        private void AStatUpdateShouldBePublishedWithLevel_(long level) =>
            PubSubHub.Verify(x => x.SendMessageAsync(It.Is<StatData>(d =>
                d.ActionType == UpdateStatActionType.UpdateLevel && d.Character.NotNull().Name == TargetName && d.Data == level)),
                Times.Once);

        private void NothingShouldBePublished() =>
            PubSubHub.Verify(x => x.SendMessageAsync(It.IsAny<StatData>()), Times.Never);

        private void TheTargetShouldBeUnsubscribed_(int times) =>
            PubSubHub.Verify(x => x.UnsubscribeAsync(TargetId), Times.Exactly(times));

        private void TheMailShouldCarryTheItemAndTheSender()
        {
            Assert.AreEqual((short?)1, SentMail.VNum);
            Assert.AreEqual((short?)1, SentMail.Amount);
            Assert.AreEqual((sbyte?)5, SentMail.Rare);
            Assert.AreEqual((byte?)7, SentMail.Upgrade);
            Assert.AreEqual(TargetId, SentMail.Mail.ReceiverId);
            Assert.AreEqual(Session.Character.VisualId, SentMail.Mail.SenderId);
            Assert.AreEqual(Session.Character.Name, SentMail.Mail.Title);
            Assert.IsNull(SentMail.Mail.ItemInstanceId);
        }

        private void NoMailShouldBeSent() => Assert.IsNull(SentMail);
    }
}
