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
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using NosCore.Shared.I18N;

namespace NosCore.Core.Encryption
{
    public class LoginDecoder : MessageToMessageDecoder<IByteBuffer>
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer message, List<object> output)
        {
            try
            {
                var decryptedPacket = new StringBuilder();

                foreach (var character in ((Span<byte>) message.Array).Slice(message.ArrayOffset, message.ReadableBytes)
                )
                {
                    decryptedPacket.Append(character > 14 ? Convert.ToChar((character - 15) ^ 195)
                        : Convert.ToChar((256 - (15 - character)) ^ 195));
                }

                output.Add(decryptedPacket.ToString());
            }
            catch
            {
                Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.ERROR_DECODING));
            }
        }
    }
}