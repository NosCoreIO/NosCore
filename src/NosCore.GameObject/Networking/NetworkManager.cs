using System;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NosCore.Core.Encryption;
using NosCore.Core.Networking;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Networking
{
	public static class NetworkManager
	{
		public static async Task RunServerAsync(int port, EncoderFactory encryptor, DecoderFactory decryptor,
			bool isWorldClient)
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
						pipeline.AddLast((MessageToMessageDecoder<IByteBuffer>) decryptor.GetDecoder());
						pipeline.AddLast(new ClientSession(channel, isWorldClient));
						pipeline.AddLast((MessageToMessageEncoder<string>) encryptor.GetEncoder());
					}));

				var bootstrapChannel = await bootstrap.BindAsync(port).ConfigureAwait(false);

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