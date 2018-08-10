using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.WebApi;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.I18N;

namespace NosCore.Controllers
{
    public class LoginPacketController : PacketController
    {
        private readonly LoginConfiguration _loginConfiguration;

        [UsedImplicitly]
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
                var connectedAccounts = new Dictionary<int, List<ConnectedAccount>>();
                var i = 1;
                foreach (var server in servers)
                {
                    var channelList = WebApiAccess.Instance.Get<List<ConnectedAccount>>($"api/connectedAccounts",
                        server.WebApi);
                    connectedAccounts.Add(i, channelList);
                    i++;
                    if (channelList.Any(a => a.Name == acc.Name))
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
                    var subpacket = new List<NsTeStSubPacket>();
                    i = 1;
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

                        var channelcolor =
                            (int)Math.Round((double)connectedAccounts[i].Count / server.ConnectedAccountsLimit * 20)
                            + 1;
                        subpacket.Add(new NsTeStSubPacket
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
                    subpacket.Add(new NsTeStSubPacket
                    {
                        Host = "-1",
                        Port = null,
                        Color = null,
                        WorldCount = 10000,
                        WorldId = 10000,
                        Name = "1"
                    }); //useless server to end the client reception
                    Session.SendPacket(new NsTestPacket
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