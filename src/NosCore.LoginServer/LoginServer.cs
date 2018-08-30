using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Client;
using NosCore.Core.Encryption;
using NosCore.Core.Networking;
using NosCore.DAL;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Polly;

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
            try
            {
                DataAccessHelper.Instance.Initialize(_loginConfiguration.Database);

                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LISTENING_PORT),
                    _loginConfiguration.Port));
                Console.Title += $" - Port : {Convert.ToInt32(_loginConfiguration.Port)}";
                NetworkManager.RunServerAsync(Convert.ToInt32(_loginConfiguration.Port), new LoginEncoderFactory(),
                    new LoginDecoderFactory(), false).Wait();
            }
            catch
            {
                Console.ReadKey();
            }
        }

        private void ConnectMaster()
        {
            async Task RunMasterClient(string targetHost, int port, string password, MasterClient clientType,
                int connectedAccountLimit = 0, int clientPort = 0, byte serverGroup = 0, string serverHost = "")
            {
                var group = new MultithreadEventLoopGroup();

                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        var pipeline = channel.Pipeline;

                        pipeline.AddLast(new LengthFieldPrepender(2));
                        pipeline.AddLast(new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                        pipeline.AddLast(new StringEncoder(), new StringDecoder());
                        pipeline.AddLast(new MasterClientSession(password));
                    }));
                var connection = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(targetHost), port))
                    .ConfigureAwait(false);

                await connection.WriteAndFlushAsync(new Channel
                {
                    Password = password,
                    ClientName = clientType.Name,
                    ClientType = (byte)clientType.Type,
                    connectedAccountLimit = connectedAccountLimit,
                    Port = clientPort,
                    ServerGroup = serverGroup,
                    Host = serverHost
                }).ConfigureAwait(false);
            }

            WebApiAccess.RegisterBaseAdress(_loginConfiguration.MasterCommunication.WebApi.ToString(), _loginConfiguration.MasterCommunication.Password);
            Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (_, __, timeSpan) =>
                    Logger.Log.Error(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MASTER_SERVER_RETRY), timeSpan.TotalSeconds))
                ).ExecuteAsync(() => RunMasterClient(_loginConfiguration.MasterCommunication.Host,
                    Convert.ToInt32(_loginConfiguration.MasterCommunication.Port),
                    _loginConfiguration.MasterCommunication.Password, 
                    new MasterClient { Name = "LoginServer", Type = ServerType.LoginServer })
                ).Wait();
        }
    }
}