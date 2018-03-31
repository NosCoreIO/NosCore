using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.Extensions.Configuration;
using NosCore.Core.Logger;
using NosCore.MasterServer;
using NosCore.Networking;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Owin;
using System.Web.Http;
using Microsoft.AspNetCore.Hosting;
using NosCore.Configuration;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Logging;
using NosCore.DAL;

namespace NosCore.Master
{
    public static class MasterServer
    {
        private static MasterConfiguration _masterConfiguration = new MasterConfiguration();

        private static string _configurationPath = @"..\..\..\configuration";

        private static void initializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory() + _configurationPath);
            builder.AddJsonFile("master.json", false);
            builder.Build().Bind(_masterConfiguration);
            Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SUCCESSFULLY_LOADED));
        }

        private static void initializeLogger()
        {
            // LOGGER
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(MasterServer)));
        }

        public static IWebHost BuildWebHost(string[] args) =>
           WebHost.CreateDefaultBuilder(args)
               .UseStartup<Startup>()
               .UseUrls(_masterConfiguration.WebApi.ToString())
               .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
               .PreferHostingUrls(true)
               .Build();

        private static void printHeader()
        {
            Console.Title = "NosCore - MasterServer";
            string text = "Master SERVER - 0Lucifer0";
            int offset = Console.WindowWidth / 2 + text.Length / 2;
            string separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }


        public static async Task RunMasterServerAsync(int port, string password)
        {
            MultithreadEventLoopGroup bossGroup = new MultithreadEventLoopGroup(1);
            MultithreadEventLoopGroup workerGroup = new MultithreadEventLoopGroup();

            try
            {

                ServerBootstrap bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                        pipeline.AddLast(new StringEncoder(), new StringDecoder());
                        pipeline.AddLast(new MasterServerSession(password));
                    }));

                IChannel bootstrapChannel = await bootstrap.BindAsync(port);

                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MASTER_SERVER_LISTENING)));
                Console.ReadLine();

                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
            }
        }

        public static void Main(string[] args)
        {
            printHeader();
            initializeLogger();
            initializeConfiguration();
            if (DataAccessHelper.Instance.Initialize(_masterConfiguration.Database))
            {
                BuildWebHost(args).StartAsync();

                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LISTENING_PORT), _masterConfiguration.Port));
                Console.Title += $" - Port : {Convert.ToInt32(_masterConfiguration.Port)} - WebApi : {(_masterConfiguration.WebApi.ToString())}";
                RunMasterServerAsync(Convert.ToInt32(_masterConfiguration.Port), _masterConfiguration.Password).Wait();
            }
        }
    }
}
