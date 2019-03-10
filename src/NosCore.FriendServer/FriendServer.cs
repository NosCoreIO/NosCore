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

using Microsoft.EntityFrameworkCore;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Database;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Threading;
using NosCore.Database.DAL;

namespace NosCore.FriendServer
{
    public class FriendServer
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly FriendConfiguration _friendServerConfiguration;

        public FriendServer(FriendConfiguration friendServerConfiguration)
        {
            _friendServerConfiguration = friendServerConfiguration;
        }

        private void ConnectMaster()
        {
            WebApiAccess.RegisterBaseAdress(new Channel
            {
                MasterCommunication = _friendServerConfiguration.MasterCommunication,
                ClientType = ServerType.FriendServer,
                ServerGroup = 0,
                WebApi = _friendServerConfiguration.WebApi
            });
        }

        public void Run()
        {
            if (_friendServerConfiguration == null)
            {
                return;
            }

            ConnectMaster();
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                //TODO save friends
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHANNEL_WILL_EXIT));
                Thread.Sleep(5000);
            };
            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            optionsBuilder.UseNpgsql(_friendServerConfiguration.Database.ConnectionString);
            DataAccessHelper.Instance.Initialize(optionsBuilder.Options);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            try
            {
                Console.Title += $" - WebApi : {_friendServerConfiguration.WebApi}";
            }
            catch
            {
                Console.ReadKey();
            }
        }
    }
}