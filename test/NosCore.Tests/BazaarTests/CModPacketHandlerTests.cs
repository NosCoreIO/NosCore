using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using Serilog;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CModPacketHandlerTest
    {
        private CModPacketHandler _cmodPacketHandler;
        private ClientSession _session;
        private Mock<IBazaarHttpClient> _bazaarHttpClient;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        [TestInitialize]
        public void Setup()
        {
            _cmodPacketHandler = new CModPacketHandler(_bazaarHttpClient.Object, _logger);
        }
    }
}
