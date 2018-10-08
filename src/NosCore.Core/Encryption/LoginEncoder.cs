using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using log4net.Repository.Hierarchy;
using NosCore.Configuration;
using NosCore.Core.Extensions;
using NosCore.Shared.I18N;
using Logger = NosCore.Shared.I18N.Logger;

namespace NosCore.Core.Encryption
{
    public class LoginEncoder : MessageToMessageEncoder<string>
    {
        private readonly LoginConfiguration _loginServerConfiguration;

        public LoginEncoder(LoginConfiguration loginServerConfiguration)
        {
            _loginServerConfiguration = loginServerConfiguration;
        }

        protected override void Encode(IChannelHandlerContext context, string message, List<object> output)
        {
            try
            {
                var tmp = _loginServerConfiguration.UserLanguage.GetEncoding().GetBytes($"{message} ");
                for (var i = 0; i < message.Length; i++)
                {
                    tmp[i] = Convert.ToByte(tmp[i] + 15);
                }

                tmp[tmp.Length - 1] = 25;
                if (tmp.Length == 0)
                {
                    return;
                }

                output.Add(Unpooled.WrappedBuffer(tmp));
            }
            catch(Exception ex)
            {
                Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.ENCODE_ERROR), ex);
            }
        }
    }
}