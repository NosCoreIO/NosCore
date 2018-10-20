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
using System.Linq;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using NosCore.Core.Extensions;
using NosCore.Core.Networking;

namespace NosCore.Core.Encryption
{
    public class WorldEncoder : MessageToMessageEncoder<string>
    {
        protected override void Encode(IChannelHandlerContext context, string message, List<object> output)
        {
            output.Add(Unpooled.WrappedBuffer(message.Split('\uffff').SelectMany(packet =>
            {
                var region = SessionFactory.Instance.Sessions[context.Channel.Id.AsLongText()].RegionType.GetEncoding();
                var strBytes = region.GetBytes(packet).AsSpan();
                var bytesLength = strBytes.Length;

                var encryptedData = new byte[bytesLength + (int) Math.Ceiling((decimal) bytesLength / 0x7E) + 1];

                var j = 0;
                for (var i = 0; i < bytesLength; i++)
                {
                    if (i % 0x7E == 0)
                    {
                        encryptedData[i + j] = (byte) (bytesLength - i > 0x7E ? 0x7E : bytesLength - i);
                        j++;
                    }

                    encryptedData[i + j] = (byte) ~strBytes[i];
                }

                encryptedData[encryptedData.Length - 1] = 0xFF;
                return encryptedData;
            }).ToArray()));
        }
    }
}