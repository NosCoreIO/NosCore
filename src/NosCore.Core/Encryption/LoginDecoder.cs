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
    public class LoginDecoder : MessageToMessageDecoder<IByteBuffer>, IDecoder
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer message, List<object> output)
        {
            try
            {
                StringBuilder decryptedPacket = new StringBuilder();

                foreach (byte character in message.Array.Skip(message.ArrayOffset).Take(message.ReadableBytes))
                {
                    decryptedPacket.Append(character > 14 ? Convert.ToChar(character - 15 ^ 195) : Convert.ToChar(256 - (15 - character) ^ 195));
                }

                output.Add(decryptedPacket.ToString());
            }
            catch
            {
            }
        }
    }
}