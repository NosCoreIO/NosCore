using System;
using System.Collections.Generic;
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Login;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Login;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class NoS0575PacketHandlerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IGenericDao<AccountDto> _accountDao = new GenericDao<Database.Entities.Account, AccountDto>(_logger);
        private readonly IGenericDao<MapDto> _mapDao = new GenericDao<Database.Entities.Map, MapDto>(_logger);
        private const string Name = "TestExistingCharacter";
        private LoginConfiguration _loginConfiguration;
        private ClientSession _session;
        private NoS0575PacketHandler _noS0575PacketHandler;
        [TestInitialize]
        public void Setup()
        {
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto { MapId = 1 };
            _mapDao.InsertOrUpdate(ref map);
            var _acc = new AccountDto { Name = Name, Password = "test".ToSha512() };
            _accountDao.InsertOrUpdate(ref _acc);
            _session = new ClientSession(_loginConfiguration, null, null, _logger, new List<IPacketHandler>());
            _session.InitializeAccount(_acc);
            _loginConfiguration = new LoginConfiguration();
            _noS0575PacketHandler = new NoS0575PacketHandler(_loginConfiguration, _accountDao);
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues = new Dictionary<WebApiRoute, object>();
        }

        [TestMethod]
        public void LoginOldClient()
        {
            _loginConfiguration.ClientData = "123456";
            _noS0575PacketHandler.Execute(new NoS0575Packet
            {
                Password = "test".ToSha512(),
                Name = Name.ToUpperInvariant()
            }, _session);
       
            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.OldClient);
        }

        [TestMethod]
        public void LoginNoAccount()
        {
            _noS0575PacketHandler.Execute(new NoS0575Packet
            {
                Password = "test".ToSha512(),
                Name = "noaccount"
            }, _session);

            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.AccountOrPasswordWrong);
        }

        [TestMethod]
        public void LoginWrongCaps()
        {
            _noS0575PacketHandler.Execute(new NoS0575Packet
            {
                Password = "test".ToSha512(),
                Name = Name.ToUpperInvariant()
            }, _session);

            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.WrongCaps);
        }

        [TestMethod]
         public void Login()
        {
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.Channel, new List<ChannelInfo> { new ChannelInfo() });
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.ConnectedAccount, new List<ConnectedAccount>());
            _noS0575PacketHandler.Execute(new NoS0575Packet
            {
                Password = "test".ToSha512(),
                Name = Name
            }, _session);

            Assert.IsTrue(_session.LastPacket is NsTestPacket);
        }

        [TestMethod]
        public void LoginAlreadyConnected()
        {
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.Channel, new List<ChannelInfo> { new ChannelInfo() });
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.ConnectedAccount,
                new List<ConnectedAccount> { new ConnectedAccount { Name = Name } });
            _noS0575PacketHandler.Execute(new NoS0575Packet
            {
                Password = "test".ToSha512(),
                Name = Name
            }, _session);
            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.AlreadyConnected);
        }

        [TestMethod]
        public void LoginNoServer()
        {
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.Channel, new List<ChannelInfo>());
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.ConnectedAccount, new List<ConnectedAccount>());
            _noS0575PacketHandler.Execute(new NoS0575Packet
            {
                Password = "test".ToSha512(),
                Name = Name
            }, _session);
            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.CantConnect);
        }

        //[TestMethod]
        //public void LoginBanned()
        //{
        //    _handler.VerifyLogin(new NoS0575Packet
        //    {
        //        Password ="test".Sha512(),
        //        Name = Name,
        //    });
        //    Assert.IsTrue(_session.LastPacket is FailcPacket);
        //    Assert.IsTrue(((FailcPacket) _session.LastPacket).Type == LoginFailType.Banned);
        //}

        //[TestMethod]
        //public void LoginMaintenance()
        //{
        //    _handler.VerifyLogin(new NoS0575Packet
        //    {
        //        Password ="test".Sha512(),
        //        Name = Name,
        //    });
        //    Assert.IsTrue(_session.LastPacket is FailcPacket);
        //    Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.Maintenance);
        //}
    }
}
