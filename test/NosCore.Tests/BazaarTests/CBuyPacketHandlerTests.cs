using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.PacketHandlers.CharacterScreen;
using Serilog;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CBuyPacketHandlerTest
    {

        private CBuyPacketHandler _cbuyPacketHandler;
        private ClientSession _session;
        private Mock<IBazaarHttpClient> _bazaarHttpClient;
        private Mock<IItemProvider> _itemProvider;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        [TestInitialize]
        public void Setup()
        {
            _cbuyPacketHandler = new CBuyPacketHandler(_bazaarHttpClient.Object, _itemProvider.Object, _logger);
        }

    }
}
