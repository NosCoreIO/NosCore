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
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.HttpClients.AuthHttpClient;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.Networking;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.Networking.LoginService
{
    public class LoginService : ILoginService
    {
        private readonly IGenericDao<AccountDto> _accountDao;
        private readonly IAuthHttpClient _authHttpClient;
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly LoginConfiguration _loginConfiguration;

        public LoginService(LoginConfiguration loginConfiguration, IGenericDao<AccountDto> accountDao,
            IAuthHttpClient authHttpClient,
            IChannelHttpClient channelHttpClient, IConnectedAccountHttpClient connectedAccountHttpClient)
        {
            _loginConfiguration = loginConfiguration;
            _accountDao = accountDao;
            _authHttpClient = authHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _channelHttpClient = channelHttpClient;
        }

        public async Task Login(string? username, string md5String, ClientVersionSubPacket clientVersion,
            ClientSession.ClientSession clientSession, string passwordToken, bool useApiAuth)
        {
            try
            {
                clientSession.SessionId = clientSession.Channel?.Id != null
                    ? SessionFactory.Instance.Sessions[clientSession.Channel.Id.AsLongText()].SessionId : 0;
                if (false) //TODO Maintenance
                {
                    await clientSession.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.Maintenance
                    });
                    await clientSession.Disconnect();
                    return;
                }

                if (((_loginConfiguration.ClientVersion != null) &&
                        (clientVersion != _loginConfiguration.ClientVersion))
                    || ((_loginConfiguration.Md5String != null) && (md5String != _loginConfiguration.Md5String)))
                {
                    await clientSession.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.OldClient
                    });
                    await clientSession.Disconnect();
                    return;
                }

                if(useApiAuth)
                {
                    username = await _authHttpClient.GetAwaitingConnection(null, passwordToken, clientSession.SessionId);
                }

                var acc = _accountDao.FirstOrDefault(s => s.Name.ToLower() == username!.ToLower());

                if ((acc != null) && (acc.Name != username))
                {
                    await clientSession.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.WrongCaps
                    });
                    await clientSession.Disconnect();
                    return;
                }

                if ((acc == null)
                    || (!useApiAuth && !string.Equals(acc.Password, passwordToken, StringComparison.OrdinalIgnoreCase)))
                {
                    await clientSession.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.AccountOrPasswordWrong
                    });
                    await clientSession.Disconnect();
                    return;
                }

                switch (acc.Authority)
                {
                    case AuthorityType.Banned:
                        await clientSession.SendPacket(new FailcPacket
                        {
                            Type = LoginFailType.Banned
                        });
                        break;
                    case AuthorityType.Closed:
                    case AuthorityType.Unconfirmed:
                        await clientSession.SendPacket(new FailcPacket
                        {
                            Type = LoginFailType.CantConnect
                        });
                        break;
                    default:
                        var servers = (await _channelHttpClient.GetChannels())
                            ?.Where(c => c.Type == ServerType.WorldServer).ToList();
                        var alreadyConnnected = false;
                        var connectedAccount = new Dictionary<int, List<ConnectedAccount>>();
                        var i = 1;
                        foreach (var server in servers ?? new List<ChannelInfo>())
                        {
                            var channelList = await _connectedAccountHttpClient.GetConnectedAccount(
                                server);
                            connectedAccount.Add(i, channelList);
                            i++;
                            if (channelList.Any(a => a.Name == acc.Name))
                            {
                                alreadyConnnected = true;
                            }
                        }

                        if (alreadyConnnected)
                        {
                            await clientSession.SendPacket(new FailcPacket
                            {
                                Type = LoginFailType.AlreadyConnected
                            });
                            await clientSession.Disconnect();
                            return;
                        }

                        acc.Language = _loginConfiguration.UserLanguage;
                        _accountDao.InsertOrUpdate(ref acc);
                        if (servers == null || servers.Count <= 0)
                        {
                            await clientSession.SendPacket(new FailcPacket
                            {
                                Type = LoginFailType.CantConnect
                            });
                            await clientSession.Disconnect();
                            return;
                        }

                        var subpacket = new List<NsTeStSubPacket?>();
                        i = 1;
                        var servergroup = string.Empty;
                        var worldCount = 1;
                        foreach (var server in servers.OrderBy(s => s.Name))
                        {
                            if (server.Name != servergroup)
                            {
                                i = 1;
                                servergroup = server.Name ?? "";
                                worldCount++;
                            }

                            var channelcolor =
                                (int) Math.Round((double) connectedAccount[i].Count / server.ConnectedAccountLimit * 20)
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

                        subpacket.Add(new NsTeStSubPacket
                        {
                            Host = "-1",
                            Port = null,
                            Color = null,
                            WorldCount = 10000,
                            WorldId = 10000,
                            Name = useApiAuth ? "4" : "1"
                        }); //useless server to end the client reception
                        await clientSession.SendPacket(new NsTestPacket
                        {
                            AccountName = username,
                            SubPacket = subpacket,
                            SessionId = clientSession.SessionId,
                            Unknown = useApiAuth ? 2 : (int?) null
                        });
                        return;
                }

                await clientSession.Disconnect();
            }
            catch
            {
                await clientSession.SendPacket(new FailcPacket
                {
                    Type = LoginFailType.UnhandledError
                });
                await clientSession.Disconnect();
            }
        }
    }
}