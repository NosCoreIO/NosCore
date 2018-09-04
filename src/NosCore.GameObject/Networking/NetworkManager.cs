using System;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Networking
{
    public class NetworkManager
    {
        private readonly GameServerConfiguration _configuration;

        public NetworkManager( GameServerConfiguration configuration)
        {
            _configuration = configuration;
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
                        SessionFactory.Instance.Sessions[channel.Id.AsLongText()] = 0;
                        var pipeline = channel.Pipeline;
                        pipeline.AddLast(DependancyResolver.Current.GetService<MessageToMessageDecoder<IByteBuffer>>());
                        var clientSession = DependancyResolver.Current.GetService<ClientSession>();
                        clientSession.RegisterChannel(channel);
                        pipeline.AddLast(clientSession);
                        pipeline.AddLast(DependancyResolver.Current.GetService<MessageToMessageEncoder<string>>());
                    }));

                var bootstrapChannel = await bootstrap.BindAsync(_configuration.Port).ConfigureAwait(false);

                Console.ReadLine();

                await bootstrapChannel.CloseAsync().ConfigureAwait(false);
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