//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using NosCore.Configuration;
using NosCore.Core.Networking;
using NosCore.Shared.I18N;

namespace NosCore.MasterServer
{
    public class MasterServer
    {
        private readonly MasterConfiguration _masterConfiguration;

        public MasterServer(MasterConfiguration masterConfiguration)
        {
            _masterConfiguration = masterConfiguration;
        }

        public void Run()
        {
            if (_masterConfiguration == null)
            {
                return;
            }

            Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SUCCESSFULLY_LOADED));
            try
            {
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LISTENING_PORT),
                    _masterConfiguration.Port));
                Console.Title +=
                    $" - Port : {Convert.ToInt32(_masterConfiguration.Port)} - WebApi : {_masterConfiguration.WebApi}";
                RunMasterServerAsync(Convert.ToInt32(_masterConfiguration.Port), _masterConfiguration.Password).Wait();
            }
            catch
            {
                Console.ReadKey();
            }
        }

        public static async Task RunMasterServerAsync(int port, string password)
        {
            var bossGroup = new MultithreadEventLoopGroup(1);
            var workerGroup = new MultithreadEventLoopGroup();

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        var pipeline = channel.Pipeline;
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                        pipeline.AddLast(new StringEncoder(), new StringDecoder());
                        pipeline.AddLast(new MasterServerSession(password));
                    }));

                var bootstrapChannel = await bootstrap.BindAsync(port);

                Logger.Log.Info(
                    string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MASTER_SERVER_LISTENING)));
                Console.ReadLine();

                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
            }
        }
    }
}