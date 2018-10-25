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
using DotNetty.Transport.Channels;
using Newtonsoft.Json;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Core.Networking
{
    public class MasterClientSession : MasterServerSession
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly Action _onConnectionLost;

        public MasterClientSession(string password, Action onConnectionLost) : base(password)
        {
            _onConnectionLost = onConnectionLost;
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
           _logger.Warning(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.UNREGISTRED_FROM_MASTER)));
            Task.Run(() => _onConnectionLost());
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            _logger.Debug(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.REGISTRED_ON_MASTER)));
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
        {
            try
            {
                var chan = JsonConvert.DeserializeObject<Channel>(msg);
                MasterClientListSingleton.Instance.ChannelId = chan.ChannelId;
            }
            catch (Exception ex)
            {
                _logger.Error(
                    string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.UNRECOGNIZED_MASTER_PACKET), ex));
            }
        }
    }
}