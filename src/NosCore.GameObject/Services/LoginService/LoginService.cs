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
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.Networking.SessionRef;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Shared.Enumerations;


namespace NosCore.GameObject.Services.LoginService
{
    public class LoginService(IOptions<LoginConfiguration> loginConfiguration, IDao<AccountDto, long> accountDao,
            IAuthHub authHttpClient,
            IPubSubHub pubSubHub, IChannelHub channelHub,
            IDao<CharacterDto, long> characterDao, ISessionRefHolder sessionRefHolder)
        : ILoginService
    {
        public async Task LoginAsync(string? username, string md5String, ClientVersionSubPacket clientVersion,
            Networking.ClientSession.ClientSession clientSession, string passwordToken, bool useApiAuth, RegionType language)
        {
            try
            {
                clientSession.SessionId = clientSession.Channel?.Id != null
                    ? sessionRefHolder[clientSession.Channel.Id.AsLongText()].SessionId : 0;


                if (((loginConfiguration.Value.ClientVersion != null) &&
                        (clientVersion != loginConfiguration.Value.ClientVersion))
                    || ((loginConfiguration.Value.Md5String != null) && (md5String != loginConfiguration.Value.Md5String)))
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
                    username = await authHttpClient.GetAwaitingConnectionAsync(null, passwordToken, clientSession.SessionId).ConfigureAwait(false);
                }

                var acc = await accountDao.FirstOrDefaultAsync(s => s.Name.ToLower() == (username ?? "").ToLower()).ConfigureAwait(false);

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
                        var connectedAccount = await pubSubHub.GetSubscribersAsync();
                        var comChannels = await channelHub.GetCommunicationChannels();
                        var servers = comChannels.Where(x => x.Type == ServerType.WorldServer).ToList();
                        if (connectedAccount.Any(x=>x.Name == acc.Name))
                        {
                            await clientSession.SendPacketAsync(new FailcPacket
                            {
                                Type = LoginFailType.AlreadyConnected
                            }).ConfigureAwait(false);
                            await clientSession.DisconnectAsync().ConfigureAwait(false);
                            return;
                        }

                        acc.Language = language;

                        acc = await accountDao.TryInsertOrUpdateAsync(acc).ConfigureAwait(false);
                        if (servers.Count <= 0)
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
                        var i = 1;
                        var nstest = new NsTestPacket
                        {
                            AccountName = username,
                            SubPacket = subpacket,
                            SessionId = clientSession.SessionId,
                            Unknown = useApiAuth ? 2 : (int?)null,
                            RegionType = language
                        };
                        var serverId = 0;
                        foreach (var server in servers.OrderBy(s => s.ServerId))
                        {
                            if (serverId != server.ServerId)
                            {
                                i = 1;
                                serverId = server.ServerId;
                            }

                            var channelcolor =
                                (int)Math.Round(connectedAccount.Count(x=>x.ChannelId == server.ServerId) / (server.ConnectedAccountLimit + 1) * 20D)
                                + 1;
                            subpacket.Add(new NsTeStSubPacket
                            {
                                Host = server.DisplayHost ?? server.Host,
                                Port = server.DisplayPort ?? server.Port,
                                Color = channelcolor,
                                WorldCount = serverId,
                                WorldId = i,
                                Name = server.Name,
                            });

                            nstest.ServerCharacters[serverId].WorldId = i;
                            nstest.ServerCharacters[serverId].CharacterCount = (byte)characterDao.Where(o =>
                                o.AccountId == acc.AccountId && o.State == CharacterState.Active &&
                                o.ServerId == serverId)!.Count();

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

                        await clientSession.SendPacketAsync(nstest).ConfigureAwait(false);
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