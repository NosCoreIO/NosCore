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
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.GameObject.Networking
{
    public class NetworkManager
    {
        private readonly ServerConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly Func<ISocketChannel, PipelineFactory> _pipelineFactory;

        public NetworkManager(ServerConfiguration configuration,
            Func<ISocketChannel, PipelineFactory> pipelineFactory, ILogger logger)
        {
            _configuration = configuration;
            _pipelineFactory = pipelineFactory;
            _logger = logger;
        }

        private static readonly AutoResetEvent _closingEvent = new AutoResetEvent(false);
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

                var bootstrapChannel = await bootstrap.BindAsync(_configuration.Port).ConfigureAwait(false);
                Console.CancelKeyPress += ((s, a) =>
                {
                    _closingEvent.Set();
                });
                _closingEvent.WaitOne();

                await bootstrapChannel.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
            finally
            {
                await Task.WhenAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync()).ConfigureAwait(false);
            }
        }
    }
}