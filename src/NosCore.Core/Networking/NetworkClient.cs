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

using DotNetty.Transport.Channels;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Packets.Interfaces;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NosCore.Core.Networking
{
    public class NetworkClient : ChannelHandlerAdapter, INetworkClient
    {
        private const short MaxPacketsBuffer = 50;
        private readonly ILogger _logger;

        public NetworkClient(ILogger logger)
        {
            _logger = logger;
            LastPackets = new ConcurrentQueue<IPacket?>();
        }

        public IChannel? Channel { get; private set; }

        public bool HasSelectedCharacter { get; set; }

        public bool IsAuthenticated { get; set; }

        public int SessionId { get; set; }
        public ConcurrentQueue<IPacket?> LastPackets { get; }

        public Task DisconnectAsync()
        {
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.FORCED_DISCONNECTION),
                SessionId);
            return Channel == null ? Task.CompletedTask : Channel.DisconnectAsync();
        }

        public Task SendPacketAsync(IPacket? packet)
        {
            return SendPacketsAsync(new[] { packet });
        }

        public async Task SendPacketsAsync(IEnumerable<IPacket?> packets)
        {
            var packetlist = packets.ToList();
            var packetDefinitions = (packets as IPacket?[] ?? packetlist.ToArray()).Where(c => c != null);
            if (!packetDefinitions.Any())
            {
                return;
            }

            await Task.WhenAll(packetlist.Select(packet => Task.Run(() => LastPackets.Enqueue(packet)))).ConfigureAwait(false);
            Parallel.For(0, LastPackets.Count - MaxPacketsBuffer, (_, __) => LastPackets.TryDequeue(out var ___));
            if (Channel == null)
            {
                return;
            }
            await Channel.WriteAndFlushAsync(packetDefinitions).ConfigureAwait(false);
        }

        public void RegisterChannel(IChannel? channel)
        {
            Channel = channel;
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        public override async void ExceptionCaught(IChannelHandlerContext context, Exception exception)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            if ((exception == null) || (context == null))
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (exception is SocketException sockException)
            {
                switch (sockException.SocketErrorCode)
                {
                    case SocketError.ConnectionReset:
                        _logger.Information(
                            LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CLIENT_DISCONNECTED),
                            SessionId);
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

            // ReSharper disable once AsyncConverter.AsyncAwaitMayBeElidedHighlighting
            await context.CloseAsync().ConfigureAwait(false);
        }
    }
}