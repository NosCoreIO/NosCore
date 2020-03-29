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

using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Packets.Interfaces;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.I18N;
using Serilog;

namespace NosCore.Core.Encryption
{
    public class LoginDecoder : MessageToMessageDecoder<IByteBuffer>
    {
        private readonly IDeserializer _deserializer;
        private readonly ILogger _logger;

        public LoginDecoder(ILogger logger, IDeserializer deserializer)
        {
            _logger = logger;
            _deserializer = deserializer;
        }

        protected override void Decode(IChannelHandlerContext context, IByteBuffer message, [NotNull] List<object> output)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            try
            {
                var decryptedPacket = new StringBuilder();
                var mapper = SessionFactory.Instance.Sessions[context.Channel.Id.AsLongText()];
                if (mapper.SessionId == 0)
                {
                    SessionFactory.Instance.Sessions[context.Channel.Id.AsLongText()].SessionId =
                        SessionFactory.Instance.GenerateSessionId();
                    _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_CONNECTED),
                        mapper.SessionId);
                }

                foreach (var character in ((Span<byte>) message.Array).Slice(message.ArrayOffset, message.ReadableBytes)
                )
                {
                    decryptedPacket.Append(character > 14 ? Convert.ToChar((character - 15) ^ 195)
                        : Convert.ToChar((256 - (15 - character)) ^ 195));
                }

                var des = _deserializer.Deserialize(decryptedPacket.ToString());
                if ((des != null) && des.IsValid)
                {
                    output?.Add(new[] {des});
                }
                else if ((des != null) && !des.IsValid)
                {
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CORRUPT_PACKET), des);
                }
                else
                {
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ERROR_DECODING,
                        decryptedPacket.ToString()));
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ERROR_DECODING), "");
            }
        }
    }
}