//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.Shared.Enumerations;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wolverine;
using IMessage = NosCore.GameObject.InterChannelCommunication.Messages.IMessage;

namespace NosCore.GameObject.Tests.InterChannelCommunication
{
    [TestClass]
    public class PubSubHubTests
    {
        private const string SenderChannel = "world-1";
        private const string OtherChannel = "world-2";
        private const string LoginChannel = "login-1";

        private readonly MessagingConfiguration Messaging = new();
        private readonly Dictionary<string, Mock<IDestinationEndpoint>> Endpoints = new();
        private PubSubHub Hub;
        private MasterClientList Clients;
        private bool Asked;

        [TestInitialize]
        public void Setup()
        {
            var clients = Clients = new MasterClientList();
            clients.Channels[SenderChannel] = new ChannelInfo { Host = "127.0.0.1", Type = ServerType.WorldServer };
            clients.Channels[OtherChannel] = new ChannelInfo { Host = "127.0.0.1", Type = ServerType.WorldServer };
            clients.Channels[LoginChannel] = new ChannelInfo { Host = "127.0.0.1", Type = ServerType.LoginServer };

            var bus = new Mock<IMessageBus>();
            foreach (var channel in clients.Channels.Keys)
            {
                var endpoint = new Mock<IDestinationEndpoint>();
                Endpoints[channel] = endpoint;
                bus.Setup(b => b.EndpointFor(Messaging.QueueUri(channel))).Returns(endpoint.Object);
            }

            Hub = new PubSubHub(clients, bus.Object, Messaging);
        }

        [TestMethod]
        public async Task PublishedMessagesReachTheSendingChannel()
        {
            await new Spec("Published messages reach the sending channel")
                .WhenAsync(AWorldChannelPublishesAMessage)
                .Then(TheChannelShouldReceiveIt_, SenderChannel)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PublishedMessagesReachTheOtherWorldChannels()
        {
            await new Spec("Published messages reach the other world channels")
                .WhenAsync(AWorldChannelPublishesAMessage)
                .Then(TheChannelShouldReceiveIt_, OtherChannel)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PublishedMessagesSkipNonWorldChannels()
        {
            await new Spec("Published messages skip non world channels")
                .WhenAsync(AWorldChannelPublishesAMessage)
                .Then(TheChannelShouldNotReceiveIt_, LoginChannel)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClosingAnAccountReachesOnlyTheChannelHoldingIt()
        {
            await new Spec("Closing an account reaches only the channel holding it")
                .Given(TheAccountIsHeldBySession_On_, 7L, OtherChannel)
                .WhenAsync(TheLoginServerAsksToCloseTheAccount)
                .Then(TheChannel_ShouldBeAskedToCloseSession_, OtherChannel, 7L)
                .And(TheChannelShouldNotReceiveIt_, SenderChannel)
                .And(TheAnswerShouldBe_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ClosingAnAccountNobodyHoldsSendsNothing()
        {
            await new Spec("Closing an account nobody holds sends nothing")
                .WhenAsync(TheLoginServerAsksToCloseTheAccount)
                .Then(TheChannelShouldNotReceiveIt_, SenderChannel)
                .And(TheChannelShouldNotReceiveIt_, OtherChannel)
                .And(TheAnswerShouldBe_, false)
                .ExecuteAsync();
        }

        private void TheAccountIsHeldBySession_On_(long sessionId, string channel)
        {
            Clients.AccountClaims["account"] = new AccountClaim(channel, sessionId);
        }

        private async Task TheLoginServerAsksToCloseTheAccount()
        {
            Asked = (await PubSubHub.Handle(new DisconnectAccountRequest("account"), Hub)).Value;
        }

        private void TheChannel_ShouldBeAskedToCloseSession_(string channel, long sessionId)
        {
            Endpoints[channel].Verify(e => e.SendAsync(It.Is<ChannelMessage>(m =>
                m.Message is DisconnectSessionData && ((DisconnectSessionData)m.Message).SessionId == sessionId
                && ((DisconnectSessionData)m.Message).AccountName == "account"), It.IsAny<DeliveryOptions?>()), Times.Once);
        }

        private void TheAnswerShouldBe_(bool asked)
        {
            Assert.AreEqual(asked, Asked);
        }

        private async Task AWorldChannelPublishesAMessage()
        {
            await PubSubHub.Handle(new PublishMessagesRequest(new List<IMessage> { new PostedPacket { Packet = string.Empty } }), Hub);
        }

        private void TheChannelShouldReceiveIt_(string channel)
        {
            Endpoints[channel].Verify(e => e.SendAsync(It.IsAny<ChannelMessage>(), It.IsAny<DeliveryOptions?>()), Times.Once);
        }

        private void TheChannelShouldNotReceiveIt_(string channel)
        {
            Endpoints[channel].Verify(e => e.SendAsync(It.IsAny<ChannelMessage>(), It.IsAny<DeliveryOptions?>()), Times.Never);
        }
    }
}
