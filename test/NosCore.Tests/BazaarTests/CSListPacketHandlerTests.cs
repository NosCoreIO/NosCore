using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Bazaar;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CSListPacketHandlerTest
    {
        private Mock<IBazaarHttpClient> _bazaarHttpClient;
        private CSListPacketHandler _cSListPacketHandler;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            _cSListPacketHandler = new CSListPacketHandler(_bazaarHttpClient.Object);
        }

        //TODO list tests
    }
}