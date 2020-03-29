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
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking;
using Serilog;

namespace NosCore.LoginServer
{
    public class LoginServer
    {
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly ILogger _logger;
        private readonly LoginConfiguration _loginConfiguration;
        private readonly NetworkManager _networkManager;

        public LoginServer(LoginConfiguration loginConfiguration, NetworkManager networkManager, ILogger logger,
            IChannelHttpClient channelHttpClient)
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
                try
                {
                    Console.Title += $@" - Port : {Convert.ToInt32(_loginConfiguration.Port)}";
                }
                catch (PlatformNotSupportedException)
                {
                    _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PLATFORM_UNSUPORTED_CONSOLE_TITLE));
                }
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LISTENING_PORT),
                    _loginConfiguration.Port);
                _networkManager.RunServerAsync().Wait();
            }
            catch
            {
                Console.ReadKey();
            }
        }
    }
}