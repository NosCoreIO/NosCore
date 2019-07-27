using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using Serilog;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Data;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CRegPacketHandlerTest
    {
        private CRegPacketHandler _cregPacketHandler;
        private ClientSession _session;
        private Mock<IBazaarHttpClient> _bazaarHttpClient;
        private Mock<IGenericDao<IItemInstanceDto>> _itemInstanceDao;
        private Mock<IGenericDao<InventoryItemInstanceDto>> _inventoryItemInstanceDao;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        [TestInitialize]
        public void Setup()
        {
            var conf = new WorldConfiguration();
            _cregPacketHandler = new CRegPacketHandler(conf, _bazaarHttpClient.Object, _itemInstanceDao.Object, _inventoryItemInstanceDao.Object);
        }
    }
}
