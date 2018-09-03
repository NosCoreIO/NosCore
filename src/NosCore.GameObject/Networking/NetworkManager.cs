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
    public static class NetworkManager
    {
        public static async Task RunServerAsync()
        {
            var configuration = DependancyResolver.Current.GetService<GameServerConfiguration>();
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
                        pipeline.AddLast(new ClientSession(channel));
                        pipeline.AddLast(DependancyResolver.Current.GetService<MessageToMessageEncoder<string>>());
                    }));

                var bootstrapChannel = await bootstrap.BindAsync(configuration.Port).ConfigureAwait(false);

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