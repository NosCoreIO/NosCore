using System;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Newtonsoft.Json;
using NosCore.Shared.I18N;

namespace NosCore.Core.Networking
{
    public class MasterClientSession : MasterServerSession
    {
        readonly Action _onConnectionLost;
        public MasterClientSession(string password, Action onConnectionLost) : base(password)
        {
            _onConnectionLost = onConnectionLost;
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            Logger.Log.Warn(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.UNREGISTRED_FROM_MASTER)));
            Task.Run(() => _onConnectionLost());
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            Logger.Log.Debug(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.REGISTRED_ON_MASTER)));
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
        {
            try
            {
                var chan = JsonConvert.DeserializeObject<Channel>(msg);
                MasterClientListSingleton.Instance.ChannelId = chan.ChannelId;
            }
            catch (Exception ex)
            {
                Logger.Log.Error(
                    string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.UNRECOGNIZED_MASTER_PACKET), ex));
            }
        }
    }
}