using System.IO;
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
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Interaction;
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
	        _handler = new LoginPacketController(new LoginConfiguration());
            _handler.RegisterSession(_session);
        }

        [TestMethod]
        public void LoginOldClient()
        {
	        _handler = new LoginPacketController(new LoginConfiguration()
	        {
				ClientData = "123456"
	        });
	        _handler.RegisterSession(_session);
            _handler.VerifyLogin(new NoS0575Packet
            {
                Password = EncryptionHelper.Sha512("test"),
                Name = Name.ToUpper(),
            });
            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.OldClient);
        }


        [TestMethod]
	    public void LoginNoAccount()
	    {
		    _handler.VerifyLogin(new NoS0575Packet
		    {
			    Password = EncryptionHelper.Sha512("test"),
			    Name = "noaccount",
		    });
		    Assert.IsTrue(_session.LastPacket is FailcPacket);
		    Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.AccountOrPasswordWrong);
	    }

        [TestMethod]
	    public void LoginWrongCaps()
	    {
		    _handler.VerifyLogin(new NoS0575Packet
		    {
			    Password = EncryptionHelper.Sha512("test"),
			    Name = Name.ToUpper(),
		    });
		    Assert.IsTrue(_session.LastPacket is FailcPacket);
		    Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.WrongCaps);
	    }

	    //[TestMethod]
	    //public void Login()
	    //{
		   // _handler.VerifyLogin(new NoS0575Packet
		   // {
			  //  Password = EncryptionHelper.Sha512("test"),
			  //  Name = Name,
		   // });
		   // Assert.IsTrue(!(_session.LastPacket is FailcPacket));
	    //}

        //[TestMethod]
        //public void LoginAlreadyConnected()
        //{
        // ServerManager.Instance.Sessions.TryAdd(_session.SessionId, _session);
        // _handler.VerifyLogin(new NoS0575Packet
        // {
        //  Password = EncryptionHelper.Sha512("test"),
        //  Name = Name,
        // });
        // Assert.IsTrue(_session.LastPacket is FailcPacket);
        // Assert.IsTrue((_session.LastPacket as FailcPacket).Type == LoginFailType.AlreadyConnected);
        //}

        //   [TestMethod]
        //   public void LoginBanned()
        //   {
        //       _handler.VerifyLogin(new NoS0575Packet
        //       {
        //           Password = EncryptionHelper.Sha512("test"),
        //           Name = Name,
        //       });
        //       Assert.IsTrue(_session.LastPacket is FailcPacket);
        //       Assert.IsTrue((_session.LastPacket as FailcPacket).Type == LoginFailType.Banned);
        //   }

        //[TestMethod]
        //public void LoginNoServer()
        //{
        // _handler.VerifyLogin(new NoS0575Packet
        // {
        //  Password = EncryptionHelper.Sha512("test"),
        //  Name = Name.ToUpper(),
        // });
        // Assert.IsTrue(_session.LastPacket is FailcPacket);
        // Assert.IsTrue((_session.LastPacket as FailcPacket).Type == LoginFailType.CantConnect);
        //}

        //Maintenance = 3,
        //WrongCountry = 8,
    }
}