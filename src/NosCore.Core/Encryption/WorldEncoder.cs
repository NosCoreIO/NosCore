using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NosCore.Core.Encryption
{
    public class WorldEncoder : MessageToMessageEncoder<string>, IEncoder
    {
        protected override void Encode(IChannelHandlerContext context, string message, List<object> output)
        {
            byte[] strBytes = Encoding.Default.GetBytes(message);
            int bytesLength = strBytes.Length;

            byte[] encryptedData = new byte[bytesLength + (int)Math.Ceiling((decimal)bytesLength / 0x7E) + 1];

            int j = 0;
            for (int i = 0; i < bytesLength; i++)
            {
                if ((i % 0x7E) == 0)
                {
                    encryptedData[i + j] = (byte)(bytesLength - i > 0x7E ? 0x7E : bytesLength - i);
                    j++;
                }
                encryptedData[i + j] = (byte)~strBytes[i];
            }
            encryptedData[encryptedData.Length - 1] = 0xFF;

            output.Add(Unpooled.WrappedBuffer(encryptedData));
        }

    }
}