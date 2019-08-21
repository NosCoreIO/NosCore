using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using System.Collections.Generic;
using NosCore.Data;
using NosCore.Tests.Helpers;
using NosCore.GameObject.Networking;
using ChickenAPI.Packets.ClientPackets.Bazaar;
using System.Linq;
using ChickenAPI.Packets.ServerPackets.Auction;
using ChickenAPI.Packets.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.Core;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CBListPacketHandlerTest
    {
        private CBListPacketHandler _cblistPacketHandler;
        private ClientSession _session;
        private Mock<IBazaarHttpClient> _bazaarHttpClient;

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            Broadcaster.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _bazaarHttpClient = new Mock<IBazaarHttpClient>();
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Main, VNum = 1012, IsSoldable = true},
            };
            _cblistPacketHandler = new CBListPacketHandler(_bazaarHttpClient.Object, items);
        }

        [TestMethod]
        public void ListShouldReturnEmptyWhenNoItems()
        {
            _bazaarHttpClient.Setup(b =>
           b.GetBazaarLinks(
           It.IsAny<int>(),
           It.IsAny<int>(),
           It.IsAny<int>(),
           It.IsAny<BazaarListType>(),
           It.IsAny<byte>(),
           It.IsAny<byte>(),
           It.IsAny<byte>(),
           It.IsAny<byte>(),
           It.IsAny<long?>())
           ).Returns(new List<BazaarLink>());
            _cblistPacketHandler.Execute(new CBListPacket { ItemVNumFilter = new List<short>() }, _session);
            var lastpacket = (RcbListPacket)_session.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(lastpacket.Items.Count == 0);
        }

        [TestMethod]
        public void ListShouldReturnTheItems()
        {
            _bazaarHttpClient.Setup(b =>
            b.GetBazaarLinks(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<BazaarListType>(),
            It.IsAny<byte>(),
            It.IsAny<byte>(),
            It.IsAny<byte>(),
            It.IsAny<byte>(),
            It.IsAny<long?>())
            ).Returns(new List<BazaarLink> {
                new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto { Price = 50, Amount = 1, DateStart = SystemTime.Now(), Duration = 200 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 1 }
               }
            });
            _cblistPacketHandler.Execute(new CBListPacket { ItemVNumFilter = new List<short>() }, _session);
            var lastpacket = (RcbListPacket)_session.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(lastpacket.Items.Count == 1);
        }

        [TestMethod]
        public void ListShouldReturnTheItemsNotValid()
        {
            _bazaarHttpClient.Setup(b =>
            b.GetBazaarLinks(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<BazaarListType>(),
            It.IsAny<byte>(),
            It.IsAny<byte>(),
            It.IsAny<byte>(),
            It.IsAny<byte>(),
            It.IsAny<long?>())
            ).Returns(new List<BazaarLink> {
                new BazaarLink
                {
                    SellerName = "test",
                    BazaarItem = new BazaarItemDto { Price = 50, Amount = 1 },
                    ItemInstance = new ItemInstanceDto { ItemVNum = 1012, Amount = 1 }
               }
            });
            _cblistPacketHandler.Execute(new CBListPacket { ItemVNumFilter = new List<short>() }, _session);
            var lastpacket = (RcbListPacket)_session.LastPackets.FirstOrDefault(s => s is RcbListPacket);
            Assert.IsTrue(lastpacket.Items.Count == 0);
        }
        //todo list filter
    }
}
