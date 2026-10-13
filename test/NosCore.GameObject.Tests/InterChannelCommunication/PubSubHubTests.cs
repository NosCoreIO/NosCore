//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
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
        private IServiceProvider Services = null!;

        [TestInitialize]
        public void Setup()
        {
            var clients = new MasterClientList();
            clients.Channels[SenderChannel] = new ChannelInfo { Type = ServerType.WorldServer };
            clients.Channels[OtherChannel] = new ChannelInfo { Type = ServerType.WorldServer };
            clients.Channels[LoginChannel] = new ChannelInfo { Type = ServerType.LoginServer };

            var bus = new Mock<IMessageBus>();
            foreach (var channel in clients.Channels.Keys)
            {
                var endpoint = new Mock<IDestinationEndpoint>();
                Endpoints[channel] = endpoint;
                bus.Setup(b => b.EndpointFor(Messaging.QueueUri(channel))).Returns(endpoint.Object);
            }

            Services = new ServiceCollection()
                .AddSingleton(new PubSubHub(clients, bus.Object, Messaging))
                .BuildServiceProvider();
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

        private async Task AWorldChannelPublishesAMessage()
        {
            await PubSubHub.Handle(new PublishMessagesRequest(new List<IMessage> { new PostedPacket() }), Services);
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
