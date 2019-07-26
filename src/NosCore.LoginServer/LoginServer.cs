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
using NosCore.Configuration;
using NosCore.GameObject.Networking;
using Serilog;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;

namespace NosCore.LoginServer
{
    public class LoginServer
    {
        private readonly ILogger _logger;
        private readonly LoginConfiguration _loginConfiguration;
        private readonly NetworkManager _networkManager;
        private readonly IChannelHttpClient _channelHttpClient;

        public LoginServer(LoginConfiguration loginConfiguration, NetworkManager networkManager, ILogger logger, IChannelHttpClient channelHttpClient)
        {
            _loginConfiguration = loginConfiguration;
            _networkManager = networkManager;
            _logger = logger;
            _channelHttpClient = channelHttpClient;
        }

        public void Run()
        {
            _channelHttpClient.Connect();
            try
            {
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LISTENING_PORT),
                    _loginConfiguration.Port);
                Console.Title += $" - Port : {Convert.ToInt32(_loginConfiguration.Port)}";
                _networkManager.RunServerAsync().Wait();
            }
            catch
            {
                Console.ReadKey();
            }
        }
    }
}