using System;
using DotNetty.Transport.Channels;
using Newtonsoft.Json;
using NosCore.Shared.I18N;

namespace NosCore.Core.Networking
{
	public class MasterClientSession : MasterServerSession
	{
		public MasterClientSession(string password) : base(password)
		{
		}

		public override void ChannelUnregistered(IChannelHandlerContext context)
		{
			Logger.Log.Warn(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.UNREGISTRED_FROM_MASTER)));
		}

		public override void ChannelRegistered(IChannelHandlerContext context)
		{
			Logger.Log.Debug(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.REGISTRED_ON_MASTER)));
		}

		protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
		{
			try
			{
				JsonConvert.DeserializeObject<Channel>(msg);
			}
			catch (Exception ex)
			{
				Logger.Log.Error(
					string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.UNRECOGNIZED_MASTER_PACKET), ex));
			}
		}
	}
}