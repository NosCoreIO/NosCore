//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.GameObject.Services.MailService;
using NosCore.GameObject.Services.MallService;
using NosCore.PacketHandlers.Shops;
using MallPacket = NosCore.Packets.ClientPackets.UI.MallPacket;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class MallPacketHandlerTests
    {
        private const long WorldChannelId = 1;
        private const short Juice = 1012;

        private TestWorld World;
        private ClientSession Session;
        private MallPacketHandler Handler;
        private IMallDeliveryService MallDelivery;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();

            World.UseReal<IParcelRegistry, ParcelRegistry>();
            World.UseReal<IMailService, MailService>();
            World.UseReal<IMailHub, MailHub>();
            World.UseReal<IMallDeliveryService, MallDeliveryService>();

            var character = Session.Character;
            World.ChannelHub.Setup(s => s.GetCommunicationChannels()).ReturnsAsync(new List<ChannelInfo>
            {
                new() { Host = "127.0.0.1", Id = WorldChannelId, Type = ServerType.WorldServer }
            });
            World.PubSubHub.Setup(s => s.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>
            {
                new()
                {
                    ChannelId = WorldChannelId, Name = character.Name,
                    ConnectedCharacter = new Character { Id = character.CharacterId, Name = character.Name }
                }
            });
            var mailArrived = World.Create<MailDataMessageChannelCommunicationMessageHandler>();
            World.PubSubHub.Setup(s => s.SendMessageAsync(It.IsAny<MailData>()))
                .Returns<MailData>(async mail =>
                {
                    await mailArrived.Handle(mail);
                    return true;
                });

            MallDelivery = World.Create<IMallDeliveryService>();
            Handler = World.Create<MallPacketHandler>();
            Session.LastPackets.Clear();
        }

        [TestMethod]
        public async Task APendingItemArrivesAsANosMallParcel()
        {
            await new Spec("A pending item arrives as a NosMall parcel")
                .GivenAsync(ThreeJuicesWerePaidFor)
                .WhenAsync(TheMallIsSynced)
                .Then(TheThanksMessageNamesTheItem)
                .And(TheParcelHoldsTheThreeJuices)
                .And(TheNewParcelIsAnnounced)
                .And(TheItemIsMarkedDelivered)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NothingPendingSendsNothing()
        {
            await new Spec("Nothing pending sends nothing")
                .WhenAsync(TheMallIsSynced)
                .Then(NoPacketIsSent)
                .And(NoMailIsWritten)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnItemIsDeliveredOnlyOnce()
        {
            await new Spec("An item is delivered only once")
                .GivenAsync(ThreeJuicesWerePaidFor)
                .AndAsync(TheMallIsSynced)
                .AndAsync(TheMallIsSyncedAgain)
                .Then(NoPacketIsSent)
                .And(OneParcelIsInTheBox)
                .ExecuteAsync();
        }

        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)50)]
        [DataRow((byte)255)]
        public async Task TheMallValueIsIgnored(byte value)
        {
            await ThreeJuicesWerePaidFor();

            await Handler.ExecuteAsync(MallPacketWith(value), Session);

            Assert.AreEqual(1, MailsOfTheCharacter().Count);
        }

        // The packet's only field is read through its type, so this compiles whatever the field is named.
        private static MallPacket MallPacketWith(byte value)
        {
            var packet = new MallPacket();
            typeof(MallPacket).GetProperties().Single(p => p.PropertyType == typeof(byte)).SetValue(packet, value);
            return packet;
        }

        private async Task ThreeJuicesWerePaidFor() =>
            Assert.IsTrue(await MallDelivery.EnqueueAsync(Session.Character.CharacterId, Juice, 3));

        private Task TheMallIsSynced() => Handler.ExecuteAsync(MallPacketWith(50), Session);

        private Task TheMallIsSyncedAgain()
        {
            Session.LastPackets.Clear();
            return TheMallIsSynced();
        }

        private void TheThanksMessageNamesTheItem() =>
            PacketAssert.AreEqual(new InfoiPacket
            {
                Message = Game18NConstString.ThanksPurchaseInNosmall,
                ArgumentType = 1,
                Game18NArguments = { Juice }
            }, Session.LastPackets.OfType<InfoiPacket>().Single());

        private void TheParcelHoldsTheThreeJuices()
        {
            var parcel = Session.LastPackets.OfType<ParcelPacket>().Single();
            Assert.AreEqual(ParcelPacketType.Listed, parcel.Type);
            Assert.AreEqual(MailBoxType.Received, parcel.Box);
            var attachment = parcel.ParcelAttachment.NotNull();
            Assert.AreEqual(1, attachment.TitleType);
            Assert.AreEqual(Juice, attachment.AttachmentVNum);
            Assert.AreEqual(3, attachment.AttachmentAmount);
        }

        private void TheNewParcelIsAnnounced()
        {
            var sayi = Session.LastPackets.OfType<SayiPacket>().Single();
            Assert.AreEqual(SayColorType.Yellow, sayi.Type);
            Assert.AreEqual(Game18NConstString.NewParcelArrived2, sayi.Message);
        }

        private void TheItemIsMarkedDelivered()
        {
            var delivery = World.MallDeliveryDao.LoadAll().Single();
            Assert.AreEqual(World.Clock.GetCurrentInstant(), delivery.DeliveredAt);
        }

        private void NoPacketIsSent() => Assert.AreEqual(0, Session.LastPackets.Count);

        private void NoMailIsWritten() => Assert.AreEqual(0, World.MailDao.LoadAll().Count());

        private void OneParcelIsInTheBox() => Assert.AreEqual(1, MailsOfTheCharacter().Count);

        private List<MailDto> MailsOfTheCharacter() =>
            World.MailDao.LoadAll().Where(m => m.ReceiverId == Session.Character.CharacterId).ToList();
    }
}
