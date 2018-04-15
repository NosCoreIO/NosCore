using System;
using DotNetty.Transport.Channels;
using Newtonsoft.Json;
using NosCore.Core.Logger;

namespace NosCore.Core.Networking
{
    public class MasterClientSession : MasterServerSession
    {
        public MasterClientSession(string password) : base(password)
        {
        }

        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            Logger.Logger.Log.Warn(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.UNREGISTRED_FROM_MASTER)));
        }

        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            Logger.Logger.Log.Debug(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.REGISTRED_ON_MASTER)));
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
        {
            Channel msgChannel;
            try
            {
                msgChannel = JsonConvert.DeserializeObject<Channel>(msg);
            }
            catch (Exception ex)
            {
                Logger.Logger.Log.Error(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.UNRECOGNIZED_MASTER_PACKET), ex));
                return;
            }
        }
    }
}
