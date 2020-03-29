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
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace NosCore.Core.I18N
{
    public static class Logger
    {
        private const string ConfigurationPath = "../../../configuration";

        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath)
            .AddYamlFile("logger.yml", false)
            .Build();

        private static readonly string[] AsciiTitle =
        {
            @" __  _  __    __   ___ __  ___ ___ ",
            @"|  \| |/__\ /' _/ / _//__\| _ \ __|",
            @"| | ' | \/ |`._`.| \_| \/ | v / _| ",
            @"|_|\__|\__/ |___/ \__/\__/|_|_\___|",
            @"-----------------------------------"
        };

        public static LoggerConfiguration GetLoggerConfiguration()
        {
            return new LoggerConfiguration().ReadFrom.Configuration(Configuration);
        }

        public static void PrintHeader(string text)
        {
            Log.Logger = GetLoggerConfiguration().CreateLogger();

            var titleLogger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}")
                .CreateLogger();
            var offset = Console.WindowWidth / 2 + text?.Length / 2;
            var separator = new string('=', Console.WindowHeight > 0 ? Console.WindowWidth - 1 : 20);
            titleLogger.Information(separator);
            foreach (var s in AsciiTitle)
            {
                titleLogger.Information(string.Format(CultureInfo.CurrentCulture, "{0," + (Console.WindowWidth / 2 + s.Length / 2) + "}", s));
            }

            titleLogger.Information(string.Format(CultureInfo.CurrentCulture, "{0," + offset + "}", text));
            titleLogger.Information(separator);
        }
    }
}