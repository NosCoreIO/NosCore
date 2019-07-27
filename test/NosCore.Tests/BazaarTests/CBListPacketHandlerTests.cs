using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using System.Collections.Generic;
using NosCore.Data;

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
            _cblistPacketHandler = new CBListPacketHandler(_bazaarHttpClient.Object, new List<ItemDto>());
        }
    }
}
