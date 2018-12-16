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
using Microsoft.EntityFrameworkCore;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Database;
using NosCore.DAL;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading;

namespace NosCore.LoginServer
{
    public class LoginServer
    {
        private readonly LoginConfiguration _loginConfiguration;
        private readonly NetworkManager _networkManager;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public LoginServer(LoginConfiguration loginConfiguration, NetworkManager networkManager)
        {
            _loginConfiguration = loginConfiguration;
            _networkManager = networkManager;
        }

        public void Run()
        {
            ConnectMaster();
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CHANNEL_WILL_EXIT));
                Thread.Sleep(5000);
            };
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
                optionsBuilder.UseNpgsql(_loginConfiguration.Database.ConnectionString);
                DataAccessHelper.Instance.Initialize(optionsBuilder.Options);
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LISTENING_PORT),
                    _loginConfiguration.Port);
                Console.Title += $" - Port : {Convert.ToInt32(_loginConfiguration.Port)}";
                _networkManager.RunServerAsync().Wait();
            }
            catch
            {
                Console.ReadKey();
            }
        }

        private void ConnectMaster()
        {
            WebApiAccess.RegisterBaseAdress(new Channel
            {
                MasterCommunication = _loginConfiguration.MasterCommunication,
                ClientType = ServerType.LoginServer,
                Port = _loginConfiguration.Port,
                Host = _loginConfiguration.Host
            });
        }
    }
}