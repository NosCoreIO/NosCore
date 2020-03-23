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
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using Serilog;
using ILogger = Serilog.ILogger;

namespace NosCore.WorldServer
{
    public static class WorldServerBootstrap
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public static void Main()
        {
            var host = BuildWebHost(new string[0]);
            // ReSharper disable PossibleNullReferenceException
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            (host.ServerFeatures[typeof(IServerAddressesFeature)] as IServerAddressesFeature).Addresses.Add(
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                (host.Services.GetService(typeof(IServerAddressesFeature)) as IServerAddressesFeature)?.Addresses
                .First());
            // ReSharper enable PossibleNullReferenceException
            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.EXCEPTION), ex.Message);
            }
        }

        private static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                })
                .UseStartup<Startup>()
                .PreferHostingUrls(true)
                .SuppressStatusMessages(true)
                .Build();
        }
    }
}