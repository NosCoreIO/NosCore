using System;
using System.Collections.Generic;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using NosCore.Core.Serializing;
using NosCore.Shared.I18N;

namespace NosCore.Core.Networking
{
	public class NetworkClient : ChannelHandlerAdapter, INetworkClient
	{
		private readonly IChannel _channel;

		#region Members

		public bool HasSelectedCharacter { get; set; }

		public bool IsAuthenticated { get; set; }

		public int SessionId { get; set; }

		public long ClientId { get; set; }
        public PacketDefinition LastPacket { get; private set; }

        public NetworkClient(IChannel channel)
		{
			_channel = channel;
		}

		#endregion

		#region Methods

		private static volatile IChannelGroup _group;

		public override void ChannelRegistered(IChannelHandlerContext context)
		{
			var g = _group;
			if (g == null)
			{
				lock (_channel)
				{
					if (_group == null)
					{
						g = _group = new DefaultChannelGroup(context.Executor);
					}
				}
			}

			Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CLIENT_CONNECTED),
				ClientId));

			g.Add(context.Channel);
		}

		public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
		{
			Logger.Log.Fatal(exception.StackTrace);
            context.CloseAsync();
		}

		public void Disconnect()
		{
			Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.FORCED_DISCONNECTION),
				ClientId));
			_channel?.DisconnectAsync();
		}

		public void SendPacket(PacketDefinition packet)
		{
			if (packet == null)
			{
				return;
			}

			LastPacket = packet;
            _channel?.WriteAndFlushAsync(PacketFactory.Serialize(packet));
			_channel?.Flush();
		}

	    public void SendPacket(string packet)
	    {
	        if (packet == null)
	        {
	            return;
	        }

	        _channel?.WriteAndFlushAsync(packet);
	        _channel?.Flush();
	    }

		public void SendPackets(IEnumerable<PacketDefinition> packets)
		{
			// TODO: maybe send at once with delimiter
			foreach (var packet in packets)
			{
				SendPacket(packet);
			}
		}

		#endregion
	}
}