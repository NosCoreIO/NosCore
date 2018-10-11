using System;
using System.Threading.Tasks;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NosCore.Configuration;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Networking
{
    public class NetworkManager
    {
        private readonly GameServerConfiguration _configuration;
        private readonly Func<ISocketChannel, PipelineFactory> _pipelineFactory;

        public NetworkManager(GameServerConfiguration configuration, Func<ISocketChannel, PipelineFactory> pipelineFactory)
        {
            _configuration = configuration;
            _pipelineFactory = pipelineFactory;
        }

        public async Task RunServerAsync()
        {
            var bossGroup = new MultithreadEventLoopGroup(1);
            var workerGroup = new MultithreadEventLoopGroup();

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        _pipelineFactory(channel).CreatePipeline();
                    }));

                var bootstrapChannel = await bootstrap.BindAsync(_configuration.Port);

                Console.ReadLine();

                await bootstrapChannel.CloseAsync();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message);
            }
            finally
            {
                Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
            }
        }
    }
}