using System;
using System.Collections.Generic;
using System.Linq;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using OpenNosCore.Core.Encryption;
using OpenNosCore.Core.Handling;
using OpenNosCore.Core.Logger;
using OpenNosCore.Core.Serializing;
using System.Reflection;
using OpenNosCore.Enum;

namespace OpenNosCore.Core.Networking
{
    public class NetworkClient : ChannelHandlerAdapter, INetworkClient
    {
        private readonly IChannel _channel;
        private readonly IEncryptor _encryptor;

        #region Members

        public bool HasSelectedCharacter { get; set; }

        public string AccountName { get; set; }

        public bool IsAuthenticated { get; set; }

        public int SessionId { get; set; }

        public AuthorityType Authority { get; set; }

        public long ClientId { get; set; }

        public NetworkClient(IEncryptor encryptor, IChannel channel)
        {
            _channel = channel;
            _encryptor = encryptor;

        }

        #endregion

        #region Methods

        private static volatile IChannelGroup _group;
  
        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            Logger.Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey("CLIENT_DISCONNECTED")));
        }

        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            IChannelGroup g = _group;
            if (g == null)
            {
                lock (this)
                {
                    if (_group == null)
                    {
                        g = _group = new DefaultChannelGroup(context.Executor);
                    }
                }
            }

            Logger.Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey("CLIENT_CONNECTED")));

            g.Add(context.Channel);
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine("{0}", e.StackTrace);

            ctx.CloseAsync();
        }


        public void Disconnect()
        {
            Logger.Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey("FORCED_DISCONNECTION")));
            _channel.DisconnectAsync();
        }



        public void SendPacket(PacketDefinition packet)
        {
            if (packet == null)
            {
                return;
            }
            IByteBuffer buffer = _channel.Allocator.Buffer();
            byte[] data = _encryptor.Encrypt(PacketFactory.Serialize(packet));
            buffer.WriteBytes(data);
            _channel.WriteAndFlushAsync(buffer);
            _channel.Flush();
        }

        public void SendPackets(IEnumerable<PacketDefinition> packets)
        {
            // TODO: maybe send at once with delimiter
            foreach (PacketDefinition packet in packets)
            {
                SendPacket(packet);
            }
        }
        #endregion

    }
}