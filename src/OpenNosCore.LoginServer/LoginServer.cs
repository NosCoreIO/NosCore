using OpenNosCore.GameHandler;
using log4net;
using log4net.Config;
using log4net.Repository;
using OpenNosCore.Core;
using OpenNosCore.Core.Encryption;
using OpenNosCore.Core.Logger;
using OpenNosCore.Core.Serializing;
using OpenNosCore.Data;
using OpenNosCore.Database;
using OpenNosCore.Packets.ClientPackets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using OpenNosCore.GameObject.Networking;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Codecs;
using DotNetty.Transport.Channels.Sockets;
using OpenNosCore.Networking;
using System.Net;
using OpenNosCore.Master.Objects;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using OpenNosCore.Configuration;

namespace OpenNosCore.LoginServer
{
    public class LoginServer
    {
        private static IEncryptor _encryptor;

        private static LoginConfiguration _loginConfiguration = new LoginConfiguration();

        private static string _configurationPath = @"..\..\..\configuration";

        private static List<IPacketHandler> _clientPacketDefinitions;

        private static void initializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory() + _configurationPath);
            builder.AddJsonFile("login.json", false);
            builder.Build().Bind(_loginConfiguration);
            Logger.Log.Info($"Login Server Configuration successfully loaded !");
        }

        private static void initializeLogger()
        {
            // LOGGER
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(LoginServer)));
        }

        private static void initializeWebApi()
        {
            var host = new WebHostBuilder()
             .UseKestrel()
             .UseStartup<Startup>()
             .Build();
            host.StartAsync();
        }

        private static void initializePackets()
        {
            _encryptor = new LoginEncryption();
            PacketFactory.Initialize<NoS0575Packet>();
            _clientPacketDefinitions = PacketFinder.GetInstancesOfImplementingTypes<IPacketHandler>(typeof(CharacterScreenPacketHandler)).ToList();
        }

        private static void connectMaster()
        {
            while (true)
            {
                try
                {
                    RunMasterClient(_loginConfiguration.MasterCommunication.Host, Convert.ToInt32(_loginConfiguration.MasterCommunication.Port), _loginConfiguration.MasterCommunication.Password, new MasterClient() { Name = "LoginServer", Type = ServerType.LoginServer }).Wait();
                    break;
                }
                catch
                {
                    Logger.Log.Error("MASTER_SERVER_RETRY");
                    Thread.Sleep(5000);
                }
            }
        }


        private static void printHeader()
        {
            Console.Title = "OpenNosCore - LoginServer";
            string text = "LOGIN SERVER - 0Lucifer0";
            int offset = Console.WindowWidth / 2 + text.Length / 2;
            string separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }

        private static void initializeMapping()
        {
            DAOFactory.AccountDAO.RegisterMapping(typeof(AccountDTO)).InitializeMapper();
        }

        public static async Task RunMasterClient(string targetHost, int port, string password, MasterClient clientType, int connectedAccountLimit = 0, int clientPort = 0, byte serverGroup = 0, string serverHost = "")
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
            var connection = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(targetHost), port));

            await connection.WriteAndFlushAsync(new Channel()
            {
                Password = password,
                ClientName = clientType.Name,
                ClientType = (byte)clientType.Type,
                ConnectedAccountsLimit = connectedAccountLimit,
                Port = clientPort,
                ServerGroup = serverGroup,
                Host = serverHost
            });

        }


        public static void Main(string[] args)
        {
            printHeader();
            initializeLogger();
            initializeConfiguration();
            initializeWebApi();
            initializeMapping();
            initializePackets();
            connectMaster();

            if (DataAccessHelper.Instance.Initialize(_loginConfiguration.Database))
            {
                NetworkManager.RunServerAsync(Convert.ToInt32(_loginConfiguration.Port), _encryptor, _clientPacketDefinitions).Wait();
                Logger.Log.Info($"Listening on port {_loginConfiguration.Port}");
                Console.Title += $" - Port : {Convert.ToInt32(_loginConfiguration.Port)} - WebApi : {(_loginConfiguration.WebApi.ToString())}";
            }
            else
            {
                Console.ReadKey();
                return;
            }
        }
    }
}