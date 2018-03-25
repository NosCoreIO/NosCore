using Newtonsoft.Json;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.DAL;
using NosCore.Data;
using NosCore.Domain.Interaction;
using NosCore.GameObject;
using NosCore.Packets;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace NosCore.GameHandler
{
    public class LoginPacketHandler : ILoginPacketHandler
    {
        #region Instantiation
        public LoginPacketHandler()
        { }
        public LoginPacketHandler(ClientSession session)
        {
            Session = session;
        }
        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        public void VerifyLogin(NoS0575Packet loginPacket)
        {
            try
            {

                if (!true)//TODO OldClient
                {
                    Session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.OldClient
                    });

                    return;
                }


                AccountDTO acc = DAOFactory.AccountDAO.FirstOrDefault(s => s.Name.ToUpper() == loginPacket.Name.ToUpper());

                if (acc != null && acc.Name != loginPacket.Name)
                {
                    Session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.WrongCaps
                    });

                    return;
                }

                if (acc == null || acc.Password.ToUpper() != loginPacket.Password.ToUpper())
                {
                    Session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.AccountOrPasswordWrong
                    });
                    return;
                }

                if (!true)//TODO Banned
                {
                    Session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.Banned
                    });

                    return;
                }

                if (!true)//TODO AlreadyConnected
                {
                    Session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.AlreadyConnected
                    });

                    return;
                }

                List<WorldServerInfo> servers = WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels");

                if (servers.Any())
                {
                    List<NsTeSTSubPacket> subpacket = new List<NsTeSTSubPacket>();
                    int i = 1;
                    string servergroup = string.Empty;
                    int worldCount = 1;
                    foreach (WorldServerInfo server in servers.OrderBy(s => s.Name))
                    {
                        if (server.Name != servergroup)
                        {
                            i = 1;
                            servergroup = server.Name;
                            worldCount++;
                        }
                        int currentlyConnectedAccounts = WebApiAccess.Instance.Get<IEnumerable<string>>($"api/connectedAccounts", server.WebApi).Count();
                        int channelcolor = (int)Math.Round((double)currentlyConnectedAccounts / server.ConnectedAccountsLimit * 20) + 1;
                        subpacket.Add(new NsTeSTSubPacket()
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
                    int newSessionId = SessionFactory.Instance.GenerateSessionId();
                    subpacket.Add(new NsTeSTSubPacket()
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

                    return;
                }

                Session.SendPacket(new FailcPacket
                {
                    Type = LoginFailType.CantConnect
                });
            }
            catch
            {
                Session.SendPacket(new FailcPacket
                {
                    Type = LoginFailType.UnhandledError
                });
            }
        }

        #endregion
    }

}
