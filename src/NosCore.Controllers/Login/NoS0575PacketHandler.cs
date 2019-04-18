//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
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
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Login;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Login
{
    public class NoS0575PacketHandler : PacketHandler<NoS0575Packet>, ILoginPacketHandler
    {
        private readonly LoginConfiguration _loginConfiguration;
        private readonly IGenericDao<AccountDto> _accountDao;

        public NoS0575PacketHandler(LoginConfiguration loginConfiguration, IGenericDao<AccountDto> accountDao)
        {
            _loginConfiguration = loginConfiguration;
            _accountDao = accountDao;
        }

        public override void Execute(NoS0575Packet packet, ClientSession session)
        {
            try
            {
                if (false) //TODO Maintenance
                {
                    session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.Maintenance
                    });
                    session.Disconnect();
                    return;
                }

                if (_loginConfiguration.ClientData != null && packet.ClientData != _loginConfiguration.ClientData)
                {
                    session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.OldClient
                    });
                    session.Disconnect();
                    return;
                }

                var acc = _accountDao.FirstOrDefault(s =>
                    string.Equals(s.Name, packet.Name, StringComparison.OrdinalIgnoreCase));

                if (acc != null && acc.Name != packet.Name)
                {
                    session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.WrongCaps
                    });
                    session.Disconnect();
                    return;
                }

                if (acc == null
                    || !string.Equals(acc.Password, packet.Password, StringComparison.OrdinalIgnoreCase))
                {
                    session.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.AccountOrPasswordWrong
                    });
                    session.Disconnect();
                    return;
                }

                switch (acc.Authority)
                {
                    case AuthorityType.Banned:
                        session.SendPacket(new FailcPacket
                        {
                            Type = LoginFailType.Banned
                        });
                        break;
                    case AuthorityType.Closed:
                    case AuthorityType.Unconfirmed:
                        session.SendPacket(new FailcPacket
                        {
                            Type = LoginFailType.CantConnect
                        });
                        break;
                    default:
                        var servers = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)
                            ?.Where(c => c.Type == ServerType.WorldServer).ToList();
                        var alreadyConnnected = false;
                        var connectedAccount = new Dictionary<int, List<ConnectedAccount>>();
                        var i = 1;
                        foreach (var server in servers ?? new List<ChannelInfo>())
                        {
                            var channelList = WebApiAccess.Instance.Get<List<ConnectedAccount>>(
                                WebApiRoute.ConnectedAccount,
                                server.WebApi);
                            connectedAccount.Add(i, channelList);
                            i++;
                            if (channelList.Any(a => a.Name == acc.Name))
                            {
                                alreadyConnnected = true;
                            }
                        }

                        if (alreadyConnnected)
                        {
                            session.SendPacket(new FailcPacket
                            {
                                Type = LoginFailType.AlreadyConnected
                            });
                            session.Disconnect();
                            return;
                        }

                        acc.Language = _loginConfiguration.UserLanguage;
                        _accountDao.InsertOrUpdate(ref acc);
                        if (servers.Count <= 0)
                        {
                            session.SendPacket(new FailcPacket
                            {
                                Type = LoginFailType.CantConnect
                            });
                            session.Disconnect();
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
                                (int)Math.Round((double)connectedAccount[i].Count / server.ConnectedAccountLimit * 20)
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
                        session.SendPacket(new NsTestPacket
                        {
                            AccountName = packet.Name,
                            SubPacket = subpacket,
                            SessionId = newSessionId
                        });
                        return;
                }

                session.Disconnect();
            }
            catch
            {
                session.SendPacket(new FailcPacket
                {
                    Type = LoginFailType.UnhandledError
                });
                session.Disconnect();
            }
        }
    }
}