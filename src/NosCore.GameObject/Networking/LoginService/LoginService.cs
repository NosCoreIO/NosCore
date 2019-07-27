using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Login;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.HttpClients.AuthHttpClient;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.Networking.LoginService
{
    public class LoginService : ILoginService
    {
        private readonly LoginConfiguration _loginConfiguration;
        private readonly IGenericDao<AccountDto> _accountDao;
        private readonly IAuthHttpClient _authHttpClient;
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;

        public LoginService(LoginConfiguration loginConfiguration, IGenericDao<AccountDto> accountDao, IAuthHttpClient authHttpClient, 
            IChannelHttpClient channelHttpClient, IConnectedAccountHttpClient connectedAccountHttpClient )
        {
            _loginConfiguration = loginConfiguration;
            _accountDao = accountDao;
            _authHttpClient = authHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _channelHttpClient = channelHttpClient;
        }

        public void Login(string username, string md5String, ClientVersionSubPacket clientVersion, ClientSession.ClientSession clientSession, string passwordToken, bool useApiAuth)
        {
            try
            {
                clientSession.SessionId = clientSession.Channel?.Id != null ? SessionFactory.Instance.Sessions[clientSession.Channel.Id.AsLongText()].SessionId : 0;
                if (false) //TODO Maintenance
                {
                    clientSession.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.Maintenance
                    });
                    clientSession.Disconnect();
                    return;
                }

                if ((_loginConfiguration.ClientVersion != null && clientVersion != _loginConfiguration.ClientVersion)
                    || (_loginConfiguration.Md5String != null && md5String != _loginConfiguration.Md5String))
                {
                    clientSession.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.OldClient
                    });
                    clientSession.Disconnect();
                    return;
                }

                var acc = _accountDao.FirstOrDefault(s =>
                    string.Equals(s.Name, username, StringComparison.OrdinalIgnoreCase));

                if (acc != null && acc.Name != username)
                {
                    clientSession.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.WrongCaps
                    });
                    clientSession.Disconnect();
                    return;
                }

                if (acc == null
                    || (!useApiAuth && !string.Equals(acc.Password, passwordToken, StringComparison.OrdinalIgnoreCase))
                    || (useApiAuth && !_authHttpClient.IsAwaitingConnection(username,passwordToken,clientSession.SessionId)))
                {
                    clientSession.SendPacket(new FailcPacket
                    {
                        Type = LoginFailType.AccountOrPasswordWrong
                    });
                    clientSession.Disconnect();
                    return;
                }

                switch (acc.Authority)
                {
                    case AuthorityType.Banned:
                        clientSession.SendPacket(new FailcPacket
                        {
                            Type = LoginFailType.Banned
                        });
                        break;
                    case AuthorityType.Closed:
                    case AuthorityType.Unconfirmed:
                        clientSession.SendPacket(new FailcPacket
                        {
                            Type = LoginFailType.CantConnect
                        });
                        break;
                    default:
                        var servers = _channelHttpClient.GetChannels()
                            ?.Where(c => c.Type == ServerType.WorldServer).ToList();
                        var alreadyConnnected = false;
                        var connectedAccount = new Dictionary<int, List<ConnectedAccount>>();
                        var i = 1;
                        foreach (var server in servers ?? new List<ChannelInfo>())
                        {
                            var channelList = _connectedAccountHttpClient.GetConnectedAccount(
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
                            clientSession.SendPacket(new FailcPacket
                            {
                                Type = LoginFailType.AlreadyConnected
                            });
                            clientSession.Disconnect();
                            return;
                        }

                        acc.Language = _loginConfiguration.UserLanguage;
                        _accountDao.InsertOrUpdate(ref acc);
                        if (servers.Count <= 0)
                        {
                            clientSession.SendPacket(new FailcPacket
                            {
                                Type = LoginFailType.CantConnect
                            });
                            clientSession.Disconnect();
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
                      
                        subpacket.Add(new NsTeStSubPacket
                        {
                            Host = "-1",
                            Port = null,
                            Color = null,
                            WorldCount = 10000,
                            WorldId = 10000,
                            Name = useApiAuth ? "4" : "1"
                        }); //useless server to end the client reception
                        clientSession.SendPacket(new NsTestPacket
                        {
                            AccountName = username,
                            SubPacket = subpacket,
                            SessionId = clientSession.SessionId,
                            Unknown = useApiAuth ? 2 : (int?) null
                        });
                        return;
                }

                clientSession.Disconnect();
            }
            catch
            {
                clientSession.SendPacket(new FailcPacket
                {
                    Type = LoginFailType.UnhandledError
                });
                clientSession.Disconnect();
            }
        }
    }
}
