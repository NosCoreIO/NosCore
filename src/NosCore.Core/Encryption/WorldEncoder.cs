using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

namespace NosCore.Core.Encryption
{
    public class WorldEncoder : MessageToMessageEncoder<string>
    {
        protected override void Encode(IChannelHandlerContext context, string message, List<object> output)
        {
            var strBytes = Encoding.Default.GetBytes(message);
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

            output.Add(Unpooled.WrappedBuffer(encryptedData));
        }
    }
}