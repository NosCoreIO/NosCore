using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

namespace NosCore.Core.Encryption
{
	public class LoginDecoder : MessageToMessageDecoder<IByteBuffer>, IDecoder
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
			}
		}
	}
}