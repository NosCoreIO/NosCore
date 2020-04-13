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
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using Serilog;
using ILogger = Serilog.ILogger;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer
{
    public static class WorldServerBootstrap
    {
        private const string ConsoleText = "WORLD SERVER - NosCoreIO";
        private const string ConfigurationPath = "../../../configuration";
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public static async Task Main()
        {
            try
            {
                await BuildWebHost(new string[0]).RunAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.EXCEPTION), ex.Message);
            }
        }

        private static IWebHost BuildWebHost(string[] args)
        {
            Logger.Initialize(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath)
                .AddYamlFile("logger.yml", false)
                .Build());
            Logger.PrintHeader(ConsoleText);
            var conf = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath)
                .AddYamlFile("world.yml", false)
                .Build();
            var webapi = conf.GetSection("WebApi");
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                })
                .UseConfiguration(conf)
                .UseUrls($"{webapi.GetValue<string>("Host")}:{webapi.GetValue<string>("Port")}")
                .UseStartup<Startup>()
                .PreferHostingUrls(true)
                .SuppressStatusMessages(true)
                .Build();
        }
    }
}