//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
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