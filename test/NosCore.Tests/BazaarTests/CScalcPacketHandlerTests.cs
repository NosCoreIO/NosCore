using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.PacketHandlers.Bazaar;
using Serilog;
using NosCore.Configuration;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Providers.ItemProvider;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CScalcPacketHandlerTest
    {
        private CScalcPacketHandler _cScalcPacketHandler;
        private ClientSession _session;
        private Mock<IBazaarHttpClient> _bazaarHttpClient;
        private Mock<IItemProvider> _itemProvider;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        [TestInitialize]
        public void Setup()
        {
            var conf = new WorldConfiguration();
            _cScalcPacketHandler = new CScalcPacketHandler(conf, _bazaarHttpClient.Object, _itemProvider.Object, _logger);
        }
    }
}
