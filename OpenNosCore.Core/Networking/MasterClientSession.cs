using DotNetty.Transport.Channels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenNosCore.Core;
using OpenNosCore.Core.Logger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OpenNosCore.Networking
{
    public class MasterClientSession : MasterServerSession
    {
        public MasterClientSession(string password) : base(password)
        {
        }

        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            Logger.Log.Warn(string.Format(LogLanguage.Instance.GetMessageFromKey("UNREGISTRED_FROM_MASTER")));
        }


        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            Logger.Log.Debug(string.Format(LogLanguage.Instance.GetMessageFromKey("REGISTRED_ON_MASTER")));
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, string toDeserialize)
        {
            Channel msg;
            try
            {
                msg = JsonConvert.DeserializeObject<Channel>(toDeserialize);
            }
            catch (Exception ex)
            {
                Logger.Log.Error(string.Format(LogLanguage.Instance.GetMessageFromKey("UNRECOGNIZED_MASTER_PACKET"), ex));
                return;
            }
        }

    }
}
