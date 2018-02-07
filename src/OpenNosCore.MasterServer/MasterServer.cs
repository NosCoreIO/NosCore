using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.Extensions.Configuration;
using OpenNosCore.Core.Logger;
using OpenNosCore.MasterServer;
using OpenNosCore.Networking;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Owin;
using System.Web.Http;
using Microsoft.AspNetCore.Hosting;
using OpenNosCore.Configuration;

namespace OpenNosCore.Master
{
    public class MasterServer
    {
        private static MasterConfiguration _masterConfiguration = new MasterConfiguration();

        private static string _configurationPath = @"..\..\..\configuration";

        private static void initializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory() + _configurationPath);
            builder.AddJsonFile("master.json", false);
            builder.Build().Bind(_masterConfiguration);
            Logger.Log.Info($"Master Server Configuration successfully loaded !");
        }

        private static void initializeLogger()
        {
            // LOGGER
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(MasterServer)));
        }

        private static void initializeWebApi()
        {
            var host = new WebHostBuilder()
             .UseKestrel()
             .UseStartup<Startup>()
             .Build();
            host.StartAsync();
        }

        private static void printHeader()
        {
            Console.Title = "OpenNosCore - MasterServer";
            string text = "Master SERVER - 0Lucifer0";
            int offset = Console.WindowWidth / 2 + text.Length / 2;
            string separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }

        private static void initializeMapping()
        {

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

                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey("MASTER_SERVER_LISTENING")));
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
            initializeWebApi();
            initializeMapping();

            Logger.Log.Info($"Listening on port {Convert.ToInt32(_masterConfiguration.Port)}");
            Console.Title += $" - Port : {Convert.ToInt32(_masterConfiguration.Port)} - WebApi : {(_masterConfiguration.WebApi)}";
            RunMasterServerAsync(Convert.ToInt32(_masterConfiguration.Port), _masterConfiguration.Password).Wait();

        }
    }
}
