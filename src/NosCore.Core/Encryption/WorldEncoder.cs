//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
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

using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using NosCore.Core.Extensions;
using NosCore.Core.Networking;
using NosCore.Packets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Networking.Extensions;

namespace NosCore.Core.Encryption
{
    public class WorldEncoder : MessageToMessageEncoder<IEnumerable<IPacket>>
    {
        private readonly ISerializer _serializer;

        public WorldEncoder(ISerializer serializer)
        {
            _serializer = serializer;
        }

        protected override void Encode(IChannelHandlerContext context, IEnumerable<IPacket> message,
            List<object> output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            output.Add(Unpooled.WrappedBuffer(message.SelectMany(packet =>
            {
                var region = SessionFactory.Instance.Sessions[context.Channel.Id.AsLongText()].RegionType.GetEncoding();
                var strBytes = region!.GetBytes(_serializer.Serialize(packet)).AsSpan();
                var bytesLength = strBytes.Length;

                var encryptedData = new byte[bytesLength + (int)Math.Ceiling((decimal)bytesLength / 0x7E) + 1];

                var j = 0;
                for (var i = 0; i < bytesLength; i++)
                {
                    if (i % 0x7E == 0)
                    {
                        encryptedData[i + j] = (byte)(bytesLength - i > 0x7E ? 0x7E : bytesLength - i);
                        j++;
                    }

                    encryptedData[i + j] = (byte)~strBytes[i];
                }

                encryptedData[^1] = 0xFF;
                return encryptedData;
            }).ToArray()));
        }
    }
}