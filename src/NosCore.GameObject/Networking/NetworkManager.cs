using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NosCore.Core.Encryption;
using NosCore.Core.Serializing;
using DotNetty.Codecs;
using System.Net;
using NosCore.Core.Logger;
using DotNetty.Buffers;
using NosCore.Core.Networking;
using NosCore.Core.Serializing.HandlerSerialization;

namespace NosCore.GameObject.Networking
{
    public static class NetworkManager
    {
        public static async Task RunServerAsync(int port, EncoderFactory encryptor, DecoderFactory decryptor, IEnumerable<IPacketHandler> packetList, bool isWorldClient)
        {
            MultithreadEventLoopGroup bossGroup = new MultithreadEventLoopGroup(1);
            MultithreadEventLoopGroup workerGroup = new MultithreadEventLoopGroup();

            try
            {
                ServerBootstrap bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        SessionFactory.Instance.Sessions[channel.Id.AsLongText()] = 0;
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast((MessageToMessageDecoder<IByteBuffer>)decryptor.GetDecoder());
                        pipeline.AddLast(new ClientSession(channel, packetList, isWorldClient));
                        pipeline.AddLast((MessageToMessageEncoder<string>)encryptor.GetEncoder());
                    }));

                IChannel bootstrapChannel = await bootstrap.BindAsync(port).ConfigureAwait(false);

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