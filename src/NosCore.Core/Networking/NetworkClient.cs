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
using System.Linq;
using System.Net.Sockets;
using ChickenAPI.Packets.Interfaces;
using DotNetty.Transport.Channels;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using Serilog;

namespace NosCore.Core.Networking
{
    public class NetworkClient : ChannelHandlerAdapter, INetworkClient
    {
        private readonly ILogger _logger;
        public NetworkClient(ILogger logger)
        {
            _logger = logger;
        }

        public IChannel Channel { get; private set; }

        public bool HasSelectedCharacter { get; set; }

        public bool IsAuthenticated { get; set; }

        public int SessionId { get; set; }
        public IPacket LastPacket { get; private set; }

        public long ClientId { get; set; }

        public void Disconnect()
        {
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.FORCED_DISCONNECTION),
                ClientId);
            Channel?.DisconnectAsync();
        }

        public void SendPacket(IPacket packet)
        {
            SendPackets(new[] {packet});
        }

        public void SendPackets(IEnumerable<IPacket> packets)
        {
            var packetDefinitions = packets as IPacket[] ?? packets.ToArray();
            if (packetDefinitions.Length == 0)
            {
                return;
            }

            LastPacket = packetDefinitions.Last();
            Channel?.WriteAndFlushAsync(packetDefinitions);
        }

        public void RegisterChannel(IChannel channel)
        {
            Channel = channel;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_CONNECTED),
                ClientId);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            if (exception is SocketException sockException)
            {
                switch (sockException.SocketErrorCode)
                {
                    case SocketError.ConnectionReset:
                        _logger.Information(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_DISCONNECTED),
                            ClientId));
                        break;
                    default:
                        _logger.Fatal(exception.StackTrace);
                        break;
                }
            }
            else
            {
                _logger.Fatal(exception.StackTrace);
            }

            context.CloseAsync();
        }
    }
}