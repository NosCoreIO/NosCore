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
using Microsoft.Extensions.Options;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.HttpClients.AuthHttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.Networking;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.WebApi;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Networking.LoginService
{
    public class LoginService : ILoginService
    {
        private readonly IDao<AccountDto, long> _accountDao;
        private readonly IAuthHttpClient _authHttpClient;
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly IOptions<LoginConfiguration> _loginConfiguration;

        public LoginService(IOptions<LoginConfiguration> loginConfiguration, IDao<AccountDto, long> accountDao,
            IAuthHttpClient authHttpClient,
            IChannelHttpClient channelHttpClient, IConnectedAccountHttpClient connectedAccountHttpClient)
        {
            _loginConfiguration = loginConfiguration;
            _accountDao = accountDao;
            _authHttpClient = authHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _channelHttpClient = channelHttpClient;
        }

        public async Task MoveChannelAsync(ClientSession.ClientSession clientSession, int channelId)
        {
            var server = await _channelHttpClient.GetChannelAsync(channelId).ConfigureAwait(false);
            if (server == null)
            {
                return;
            }
            await clientSession.SendPacketAsync(new MzPacket
            {
                Port = server.DisplayPort ?? server.Port,
                Ip = server.DisplayHost ?? server.Host,
                CharacterSlot = clientSession.Character.Slot
            });

            await clientSession.SendPacketAsync(new ItPacket
            {
                Mode = 1
            });

            await _authHttpClient.SetAwaitingConnectionAsync(-1, clientSession.Account.Name);
            await clientSession.Character.SaveAsync();
            await clientSession.DisconnectAsync();
        }

        public async Task LoginAsync(string? username, string md5String, ClientVersionSubPacket clientVersion,
            ClientSession.ClientSession clientSession, string passwordToken, bool useApiAuth, RegionType language)
        {
            try
            {
                clientSession.SessionId = clientSession.Channel?.Id != null
                    ? SessionFactory.Instance.Sessions[clientSession.Channel.Id.AsLongText()].SessionId : 0;


                if (((_loginConfiguration.Value.ClientVersion != null) &&
                        (clientVersion != _loginConfiguration.Value.ClientVersion))
                    || ((_loginConfiguration.Value.Md5String != null) && (md5String != _loginConfiguration.Value.Md5String)))
                {
                    await clientSession.SendPacketAsync(new FailcPacket
                    {
                        Type = LoginFailType.OldClient
                    }).ConfigureAwait(false);
                    await clientSession.DisconnectAsync().ConfigureAwait(false);
                    return;
                }

                if (useApiAuth)
                {
                    username = await _authHttpClient.GetAwaitingConnectionAsync(null, passwordToken, clientSession.SessionId).ConfigureAwait(false);
                }

                var acc = await _accountDao.FirstOrDefaultAsync(s => s.Name.ToLower() == (username ?? "").ToLower()).ConfigureAwait(false);

                if ((acc != null) && (acc.Name != username))
                {
                    await clientSession.SendPacketAsync(new FailcPacket
                    {
                        Type = LoginFailType.WrongCaps
                    }).ConfigureAwait(false);
                    await clientSession.DisconnectAsync().ConfigureAwait(false);
                    return;
                }

                if ((acc == null)
                    || (!useApiAuth && !string.Equals(acc.Password, passwordToken, StringComparison.OrdinalIgnoreCase)))
                {
                    await clientSession.SendPacketAsync(new FailcPacket
                    {
                        Type = LoginFailType.AccountOrPasswordWrong
                    }).ConfigureAwait(false);
                    await clientSession.DisconnectAsync().ConfigureAwait(false);
                    return;
                }

                switch (acc.Authority)
                {
                    case AuthorityType.Banned:
                        await clientSession.SendPacketAsync(new FailcPacket
                        {
                            Type = LoginFailType.Banned
                        }).ConfigureAwait(false);
                        break;
                    case AuthorityType.Closed:
                    case AuthorityType.Unconfirmed:
                        await clientSession.SendPacketAsync(new FailcPacket
                        {
                            Type = LoginFailType.CantConnect
                        }).ConfigureAwait(false);
                        break;
                    default:
                        var servers = (await _channelHttpClient.GetChannelsAsync().ConfigureAwait(false))
                            ?.Where(c => c.Type == ServerType.WorldServer).ToList();
                        var alreadyConnnected = false;
                        var connectedAccount = new Dictionary<int, List<ConnectedAccount>>();
                        var i = 1;
                        foreach (var server in servers ?? new List<ChannelInfo>())
                        {
                            var channelList = await _connectedAccountHttpClient.GetConnectedAccountAsync(
                                server).ConfigureAwait(false);
                            connectedAccount.Add(i, channelList);
                            i++;
                            if (channelList.Any(a => a.Name == acc.Name))
                            {
                                alreadyConnnected = true;
                            }
                        }

                        if (alreadyConnnected)
                        {
                            await clientSession.SendPacketAsync(new FailcPacket
                            {
                                Type = LoginFailType.AlreadyConnected
                            }).ConfigureAwait(false);
                            await clientSession.DisconnectAsync().ConfigureAwait(false);
                            return;
                        }

                        acc.Language = language;

                        acc = await _accountDao.TryInsertOrUpdateAsync(acc).ConfigureAwait(false);
                        if (servers == null || servers.Count <= 0)
                        {
                            await clientSession.SendPacketAsync(new FailcPacket
                            {
                                Type = LoginFailType.CantConnect
                            }).ConfigureAwait(false);
                            await clientSession.DisconnectAsync().ConfigureAwait(false);
                            return;
                        }

                        if (servers.Count(s => !s.IsMaintenance) == 0 && acc.Authority < AuthorityType.GameMaster)
                        {
                            await clientSession.SendPacketAsync(new FailcPacket
                            {
                                Type = LoginFailType.Maintenance
                            });
                            await clientSession.DisconnectAsync();
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
                                (int)Math.Round((double)connectedAccount[i].Count / server.ConnectedAccountLimit * 20)
                                + 1;
                            subpacket.Add(new NsTeStSubPacket
                            {
                                Host = server.DisplayHost ?? server.Host,
                                Port = server.DisplayPort ?? server.Port,
                                Color = channelcolor,
                                WorldCount = worldCount,
                                WorldId = i,
                                Name = server.Name,
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
                        await clientSession.SendPacketAsync(new NsTestPacket
                        {
                            AccountName = username,
                            SubPacket = subpacket,
                            SessionId = clientSession.SessionId,
                            Unknown = useApiAuth ? 2 : (int?)null,
                            RegionType = language
                        }).ConfigureAwait(false);
                        return;
                }

                await clientSession.DisconnectAsync().ConfigureAwait(false);
            }
            catch
            {
                await clientSession.SendPacketAsync(new FailcPacket
                {
                    Type = LoginFailType.UnhandledError
                }).ConfigureAwait(false);
                await clientSession.DisconnectAsync().ConfigureAwait(false);
            }
        }
    }
}