using System;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class LoginPacketControllerTests
    {
        private const string ConfigurationPath = "../../../configuration";
        private readonly ClientSession _session = new ClientSession();
        private AccountDTO _acc;
	    private LoginPacketController _handler;
	    private const string Name = "TestExistingCharacter";

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var builder = new ConfigurationBuilder();
            var databaseConfiguration = new SqlConnectionConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("database.json", false);
            builder.Build().Bind(databaseConfiguration);
            databaseConfiguration.Database = "postgresunittest";
            var sqlconnect = databaseConfiguration;
            DataAccessHelper.Instance.EnsureDeleted(sqlconnect);
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(ConfigurationPath + "/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(LoginPacketController)));
            DataAccessHelper.Instance.Initialize(sqlconnect);
            DAOFactory.RegisterMapping(typeof(Character).Assembly);
            var map = new MapDTO { MapId = 1 };
            DAOFactory.MapDAO.InsertOrUpdate(ref map);
            _acc = new AccountDTO { Name = Name, Password = EncryptionHelper.Sha512("test") };
            DAOFactory.AccountDAO.InsertOrUpdate(ref _acc);
            _session.InitializeAccount(_acc);
	        _handler = new LoginPacketController(new LoginConfiguration()
	        {

	        });
            _handler.RegisterSession(_session);
        }

	    [TestMethod]
        public void Login()
        {
            _handler.VerifyLogin(new NoS0575Packet
            {
                Password = "test",
                Name = Name,
            });
            Assert.IsTrue(_session.LastPacket is FailcPacket);
        }
    }
}