using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.Extensions.Configuration;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core;
using NosCore.Core.Client;
using NosCore.Core.Encryption;
using NosCore.Core.Extensions;
using NosCore.Core.Logger;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.DAL;
using NosCore.Domain;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.LoginServer
{
    public class LoginServer
    {
        private readonly LoginConfiguration _loginConfiguration;

        public LoginServer(LoginConfiguration loginConfiguration)
        {
            _loginConfiguration = loginConfiguration;
        }

        public void Run()
        {
            ConnectMaster();
            if (DataAccessHelper.Instance.Initialize(_loginConfiguration.Database))
            {
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LISTENING_PORT), _loginConfiguration.Port));
                Console.Title += $" - Port : {Convert.ToInt32(_loginConfiguration.Port)}";
                NetworkManager.RunServerAsync(Convert.ToInt32(_loginConfiguration.Port), new LoginEncoderFactory(), new LoginDecoderFactory(), false).Wait();
            }
            else
            {
                Console.ReadKey();
                return;
            }
        }

        private void ConnectMaster()
        {
            async Task RunMasterClient(string targetHost, int port, string password, MasterClient clientType, int connectedAccountLimit = 0, int clientPort = 0, byte serverGroup = 0, string serverHost = "")
            {
                var group = new MultithreadEventLoopGroup();

                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        pipeline.AddLast(new LengthFieldPrepender(2));
                        pipeline.AddLast(new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                        pipeline.AddLast(new StringEncoder(), new StringDecoder());
                        pipeline.AddLast(new MasterClientSession(password));
                    }));
                var connection = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(targetHost), port)).ConfigureAwait(false);

                await connection.WriteAndFlushAsync(new Channel()
                {
                    Password = password,
                    ClientName = clientType.Name,
                    ClientType = (byte)clientType.Type,
                    ConnectedAccountsLimit = connectedAccountLimit,
                    Port = clientPort,
                    ServerGroup = serverGroup,
                    Host = serverHost
                }).ConfigureAwait(false);
            }

            while (true)
            {
                try
                {
                    WebApiAccess.RegisterBaseAdress(_loginConfiguration.MasterCommunication.WebApi.ToString());
                    RunMasterClient(_loginConfiguration.MasterCommunication.Host, Convert.ToInt32(_loginConfiguration.MasterCommunication.Port), _loginConfiguration.MasterCommunication.Password, new MasterClient() { Name = "LoginServer", Type = ServerType.LoginServer }).Wait();
                    break;
                }
                catch
                {
                    Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_RETRY));
                    Thread.Sleep(5000);
                }
            }
        }
    }
}