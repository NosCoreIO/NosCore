using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using log4net;
using log4net.Config;
using log4net.Repository;
using NosCore.Configuration;
using NosCore.Core.Logger;
using NosCore.Core.Networking;
using NosCore.DAL;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace NosCore.MasterServer
{
    public class MasterServer
    {
        private readonly MasterConfiguration _masterConfiguration;

        public MasterServer(MasterConfiguration masterConfiguration)
        {
            _masterConfiguration = masterConfiguration;
        }

        private void InitializeLogger()
        {
            // LOGGER
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(MasterServer)));
        }

        public void Run()
        {
            InitializeLogger();
            if (_masterConfiguration != null)
            {
                Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            }
            if (DataAccessHelper.Instance.Initialize(_masterConfiguration.Database))
            {
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LISTENING_PORT), _masterConfiguration.Port));
                Console.Title += $" - Port : {Convert.ToInt32(_masterConfiguration.Port)} - WebApi : {_masterConfiguration.WebApi}";
                RunMasterServerAsync(Convert.ToInt32(_masterConfiguration.Port), _masterConfiguration.Password).Wait();
            }
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

                IChannel bootstrapChannel = await bootstrap.BindAsync(port).ConfigureAwait(false);

                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_LISTENING)));
                Console.ReadLine();

                await bootstrapChannel.CloseAsync().ConfigureAwait(false);
            }
            finally
            {
                Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
            }
        }
    }
}
