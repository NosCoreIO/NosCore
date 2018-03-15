using System;
using System.Collections.Generic;
using System.Linq;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using NosCore.Core.Encryption;
using NosCore.Core.Handling;
using NosCore.Core.Logger;
using NosCore.Core.Serializing;
using System.Reflection;
using NosCore.Enum;

namespace NosCore.Core.Networking
{
    public class NetworkClient : ChannelHandlerAdapter, INetworkClient
    {
        private readonly IChannel _channel;

        #region Members

        public bool HasSelectedCharacter { get; set; }

        public string AccountName { get; set; }

        public bool IsAuthenticated { get; set; }
        
        public int SessionId { get; set; }

        public AuthorityType Authority { get; set; }

        public long ClientId { get; set; }

        public NetworkClient(IChannel channel)
        {
            _channel = channel;

        }

        #endregion

        #region Methods

        private static volatile IChannelGroup _group;
  
        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            SessionFactory.Instance.Sessions.TryRemove(context.Channel.Id.AsLongText(), out int i);
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
            _channel.WriteAndFlushAsync(PacketFactory.Serialize(packet));
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