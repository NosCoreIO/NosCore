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

                encryptedData[encryptedData.Length - 1] = 0xFF;
                return encryptedData;
            }).ToArray()));
        }
    }
}