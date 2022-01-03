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

using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Options;
using NosCore.Shared.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NosCore.Networking;

namespace NosCore.GameObject.Networking
{
    public class NetworkManager
    {
        private readonly IOptions<ServerConfiguration> _configuration;
        private readonly ILogger<NetworkManager> _logger;
        private readonly Func<ISocketChannel, IPipelineFactory> _pipelineFactory;

        public NetworkManager(IOptions<ServerConfiguration> configuration,
            Func<ISocketChannel, IPipelineFactory> pipelineFactory, ILogger<NetworkManager> logger)
        {
            _configuration = configuration;
            _pipelineFactory = pipelineFactory;
            _logger = logger;
        }

        private static readonly AutoResetEvent ClosingEvent = new AutoResetEvent(false);
        public async Task RunServerAsync()
        {
            var bossGroup = new MultithreadEventLoopGroup(1);
            var workerGroup = new MultithreadEventLoopGroup();

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                        _pipelineFactory(channel).CreatePipeline()));

                var bootstrapChannel = await bootstrap.BindAsync(_configuration.Value.Port).ConfigureAwait(false);
                Console.CancelKeyPress += ((s, a) =>
                {
                    ClosingEvent.Set();
                });
                ClosingEvent.WaitOne();

                await bootstrapChannel.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                await Task.WhenAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync()).ConfigureAwait(false);
            }
        }
    }
}