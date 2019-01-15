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
using System.Linq;
using GraphQL;
using GraphQL.Client;
using GraphQL.Common.Request;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.GraphQl;
using NosCore.Data.WebApi;
using NosCore.DAL;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.Enumerations;
using Mapster;
using Newtonsoft.Json.Linq;

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

                var acc = DaoFactory.AccountDao.FirstOrDefault(s =>
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

                switch (acc.Authority)
                {
                    case AuthorityType.Banned:
                        Session.SendPacket(new FailcPacket
                        {
                            Type = LoginFailType.Banned
                        });
                        break;
                    case AuthorityType.Closed:
                    case AuthorityType.Unconfirmed:
                        Session.SendPacket(new FailcPacket
                        {
                            Type = LoginFailType.CantConnect
                        });
                        break;
                    default:
                        var servers = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)?.Where(c=>c.Type == ServerType.WorldServer).ToList();
                        var alreadyConnnected = false;
                        var connectedAccountCount = new Dictionary<int, int>();
                        var i = 1;
                        var connectedAccountRequest = new GraphQLRequest { Query = "{ connectedAccounts { name } }" };
                        foreach (var server in servers ?? new List<ChannelInfo>())
                        {
                            var graphQlClient = new GraphQLClient($"{server.WebApi}/api/graphql");
                            var graphQlResponse = graphQlClient.PostAsync(connectedAccountRequest).Result; //TODO move to async
                            var connected = graphQlResponse.Data.connectedAccounts as JArray;
                            connectedAccountCount.Add(i, connected.Count);
                            i++;
                            if (connected.Select(ACC => acc.GetPropertyValue("name")).Any(account => account.GetValue().ToString() == acc.Name))
                            {
                                alreadyConnnected = true;
                                break;
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
                        DaoFactory.AccountDao.InsertOrUpdate(ref acc);
                        if (servers.Count <= 0)
                        {
                            Session.SendPacket(new FailcPacket
                            {
                                Type = LoginFailType.CantConnect
                            });
                            Session.Disconnect();
                            return;
                        }

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
                                (int)Math.Round((double)connectedAccountCount[i] / server.ConnectedAccountLimit * 20)
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
                        return;
                }

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