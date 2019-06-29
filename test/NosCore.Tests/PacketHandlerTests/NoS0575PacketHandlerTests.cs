using System.Collections.Generic;
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Login;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Login;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class NoS0575PacketHandlerTests
    {
        private LoginConfiguration _loginConfiguration;
        private ClientSession _session;
        private NoS0575PacketHandler _noS0575PacketHandler;
        private Mock<IWebApiAccess> _webApiAccess;
        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _loginConfiguration = new LoginConfiguration();
            _webApiAccess = new Mock<IWebApiAccess>();
            _noS0575PacketHandler = new NoS0575PacketHandler(new LoginService(_loginConfiguration, TestHelpers.Instance.AccountDao, _webApiAccess.Object));
        }

        [TestMethod]
        public void LoginOldClient()
        {
            _loginConfiguration.ClientVersion = new ClientVersionSubPacket() {Major = 1};
            _noS0575PacketHandler.Execute(new NoS0575Packet
            {
                Password = "test".ToSha512(),
                Name = _session.Account.Name.ToUpperInvariant()
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
                Name = _session.Account.Name.ToUpperInvariant()
            }, _session);

            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.WrongCaps);
        }

        [TestMethod]
        public void Login()
        {
            _webApiAccess.Setup(s => s.Get<List<ChannelInfo>>(WebApiRoute.Channel)).Returns(new List<ChannelInfo> { new ChannelInfo() });
            _webApiAccess.Setup(s => s.Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, It.IsAny<ServerConfiguration>())).Returns(new List<ConnectedAccount>());
            _noS0575PacketHandler.Execute(new NoS0575Packet
            {
                Password = "test".ToSha512(),
                Name = _session.Account.Name
            }, _session);

            Assert.IsTrue(_session.LastPacket is NsTestPacket);
        }

        [TestMethod]
        public void LoginAlreadyConnected()
        {
            _webApiAccess.Setup(s => s.Get<List<ChannelInfo>>(WebApiRoute.Channel)).Returns(new List<ChannelInfo> { new ChannelInfo() });
            _webApiAccess.Setup(s => s.Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, It.IsAny<ServerConfiguration>())).Returns(new List<ConnectedAccount>
                {new ConnectedAccount {Name = _session.Account.Name}});
            _noS0575PacketHandler.Execute(new NoS0575Packet
            {
                Password = "test".ToSha512(),
                Name = _session.Account.Name
            }, _session);
            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.AlreadyConnected);
        }
         
        [TestMethod]
        public void LoginNoServer()
        {
            _webApiAccess.Setup(s => s.Get<List<ChannelInfo>>(WebApiRoute.Channel)).Returns(new List<ChannelInfo>());
            _webApiAccess.Setup(s => s.Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount)).Returns(new List<ConnectedAccount>());
            _noS0575PacketHandler.Execute(new NoS0575Packet
            {
                Password = "test".ToSha512(),
                Name = _session.Account.Name
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
