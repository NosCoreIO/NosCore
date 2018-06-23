using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Interaction;

namespace NosCore.Controllers
{
    public class LoginPacketController : PacketController
    {
        private readonly LoginConfiguration _loginConfiguration;

        public LoginPacketController()
        {
        }

        public LoginPacketController(LoginConfiguration loginConfiguration)
        {
            _loginConfiguration = loginConfiguration;
        }

        public void VerifyLogin(NoS0575Packet loginPacket)
        {
            try
            {

	            if (false) //TODO Maintenance
                {
		            Session.SendPacket(new FailcPacket
		            {
			            Type = LoginFailType.Maintenance
		            });
		            Session.Disconnect();
		            return;
	            }

                if (_loginConfiguration.ClientData != null && loginPacket.ClientData != _loginConfiguration.ClientData)
                {
                    Session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.OldClient
                    });
	                Session.Disconnect();
                    return;
                }

                var acc = DAOFactory.AccountDAO.FirstOrDefault(s =>
                    string.Equals(s.Name, loginPacket.Name, StringComparison.OrdinalIgnoreCase));

                if (acc != null && acc.Name != loginPacket.Name)
                {
                    Session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.WrongCaps
                    });
	                Session.Disconnect();
                    return;
                }

                if (acc == null
                    || !string.Equals(acc.Password, loginPacket.Password, StringComparison.OrdinalIgnoreCase))
                {
                    Session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.AccountOrPasswordWrong
                    });
					Session.Disconnect();
                    return;
                }

                if (false) //TODO Banned
                {
                    Session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.Banned
                    });
	                Session.Disconnect();
                    return;
                }

                var servers = WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels");
                var alreadyConnnected = false;
                foreach (var server in servers)
                {
                    if (WebApiAccess.Instance.Get<IEnumerable<string>>($"api/connectedAccounts", server.WebApi).Any(a => a == acc.Name))
                    {
                        alreadyConnnected = true;
                    }
                }

                if (alreadyConnnected)
                {
                    Session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.AlreadyConnected
                    });
	                Session.Disconnect();
                    return;
                }

                acc.Language = _loginConfiguration.UserLanguage;
                DAOFactory.AccountDAO.InsertOrUpdate(ref acc);

                if (servers.Count > 0)
                {
                    var subpacket = new List<NsTeSTSubPacket>();
                    var i = 1;
                    var servergroup = string.Empty;
                    var worldCount = 1;
                    foreach (var server in servers.OrderBy(s => s.Name))
                    {
                        if (server.Name != servergroup)
                        {
                            i = 1;
                            servergroup = server.Name;
                            worldCount++;
                        }

                        var currentlyConnectedAccounts = WebApiAccess.Instance
                            .Get<IEnumerable<string>>($"api/connectedAccounts", server.WebApi).Count();
                        var channelcolor =
                            (int)Math.Round((double)currentlyConnectedAccounts / server.ConnectedAccountsLimit * 20)
                            + 1;
                        subpacket.Add(new NsTeSTSubPacket
                        {
                            Host = server.Host,
                            Port = server.Port,
                            Color = channelcolor,
                            WorldCount = worldCount,
                            WorldId = i,
                            Name = server.Name
                        });
                        i++;
                    }

                    var newSessionId = SessionFactory.Instance.GenerateSessionId();
                    subpacket.Add(new NsTeSTSubPacket
                    {
                        Host = "-1",
                        Port = null,
                        Color = null,
                        WorldCount = 10000,
                        WorldId = 10000,
                        Name = "1"
                    }); //useless server to end the client reception
                    Session.SendPacket(new NSTestPacket
                    {
                        AccountName = loginPacket.Name,
                        SubPacket = subpacket,
                        SessionId = newSessionId
                    });
	                Session.Disconnect();
                    return;
                }

                Session.SendPacket(new FailcPacket
                {
                    Type = LoginFailType.CantConnect
                });
	            Session.Disconnect();
            }
            catch
            {
                Session.SendPacket(new FailcPacket
                {
                    Type = LoginFailType.UnhandledError
                });
	            Session.Disconnect();
            }
        }
    }
}