using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using DotNetty.Transport.Channels;
using NosCore.Core.Serializing;
using NosCore.Shared.I18N;

namespace NosCore.Core.Networking
{
    public class NetworkClient : ChannelHandlerAdapter, INetworkClient
    {
        private IChannel _channel;

        #region Members

        public bool HasSelectedCharacter { get; set; }

        public bool IsAuthenticated { get; set; }

        public int SessionId { get; set; }

        public long ClientId { get; set; }
        public PacketDefinition LastPacket { get; private set; }

        public void RegisterChannel(IChannel channel)
        {
            _channel = channel;
        }

        #endregion

        #region Methods

        public override void ChannelActive(IChannelHandlerContext context)
        {
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CLIENT_CONNECTED),
                ClientId));
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            if (exception is SocketException sockException)
            {
                switch (sockException.SocketErrorCode)
                {
                    case SocketError.ConnectionReset:
                        Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CLIENT_DISCONNECTED),
                            ClientId));
                        break;
                    default:
                        Logger.Log.Fatal(exception.StackTrace);
                        break;
                }
            }
            else { Logger.Log.Fatal(exception.StackTrace); }

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
            SendPackets(new[] { packet });
        }

        public void SendPackets(IEnumerable<PacketDefinition> packets)
        {
            var packetDefinitions = packets as PacketDefinition[] ?? packets.ToArray();
            if (packetDefinitions.Length == 0)
            {
                return;
            }

            LastPacket = packetDefinitions.Last();
            _channel?.WriteAndFlushAsync(PacketFactory.Serialize(packetDefinitions));
        }

        #endregion
    }
}