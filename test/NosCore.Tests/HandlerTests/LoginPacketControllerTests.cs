//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.GraphQl;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.DAL;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared;
using NosCore.Shared.Enumerations.Interaction;

namespace NosCore.Tests.HandlerTests
{
    [TestClass]
    public class LoginPacketControllerTests
    {
        private const string Name = "TestExistingCharacter";

        private readonly ClientSession _session =
            new ClientSession(null, new List<PacketController> { new LoginPacketController() }, null, null);

        private LoginPacketController _handler;

        [TestInitialize]
        public void Setup()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);
            var map = new MapDto { MapId = 1 };
            DaoFactory.MapDao.InsertOrUpdate(ref map);
            var _acc = new AccountDto { Name = Name, Password = "test".ToSha512() };
            DaoFactory.AccountDao.InsertOrUpdate(ref _acc);
            _session.InitializeAccount(_acc);
            _handler = new LoginPacketController(new LoginConfiguration());
            _handler.RegisterSession(_session);
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues = new Dictionary<WebApiRoute, object>();
        }

        [TestMethod]
        public void LoginOldClient()
        {
            _handler = new LoginPacketController(new LoginConfiguration
            {
                ClientData = "123456"
            });
            _handler.RegisterSession(_session);
            _handler.VerifyLogin(new NoS0575Packet
            {
                Password ="test".ToSha512(),
                Name = Name.ToUpperInvariant()
            });
            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.OldClient);
        }

        [TestMethod]
        public void LoginNoAccount()
        {
            _handler.VerifyLogin(new NoS0575Packet
            {
                Password ="test".ToSha512(),
                Name = "noaccount"
            });
            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.AccountOrPasswordWrong);
        }

        [TestMethod]
        public void LoginWrongCaps()
        {
            _handler.VerifyLogin(new NoS0575Packet
            {
                Password ="test".ToSha512(),
                Name = Name.ToUpperInvariant()
            });
            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.WrongCaps);
        }

        [TestMethod]
        public void Login()
        {
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.Channel, new List<ChannelInfo> { new ChannelInfo() });
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.ConnectedAccount, new List<ConnectedAccount>());
            _handler.VerifyLogin(new NoS0575Packet
            {
                Password ="test".ToSha512(),
                Name = Name
            });
            Assert.IsTrue(_session.LastPacket is NsTestPacket);
        }

        [TestMethod]
        public void LoginAlreadyConnected()
        {
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.Channel, new List<ChannelInfo> { new ChannelInfo() });
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.ConnectedAccount,
                new List<ConnectedAccount> { new ConnectedAccount { Name = Name } });
            _handler.VerifyLogin(new NoS0575Packet
            {
                Password ="test".ToSha512(),
                Name = Name
            });
            Assert.IsTrue(_session.LastPacket is FailcPacket);
            Assert.IsTrue(((FailcPacket)_session.LastPacket).Type == LoginFailType.AlreadyConnected);
        }

        [TestMethod]
        public void LoginNoServer()
        {
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.Channel, new List<ChannelInfo>());
            WebApiAccess.Instance.MockValues.Add(WebApiRoute.ConnectedAccount, new List<ConnectedAccount>());
            _handler.VerifyLogin(new NoS0575Packet
            {
                Password ="test".ToSha512(),
                Name = Name
            });
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