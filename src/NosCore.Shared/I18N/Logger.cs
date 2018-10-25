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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NosCore.Shared.I18N.Enrichers;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace NosCore.Shared.I18N
{
    public static class Logger
    {
        public static LoggerConfiguration GetLoggerConfiguration()
        {
            return new LoggerConfiguration()
                .Enrich.With<ShortLevelMapperEnricher>()
                .WriteTo.Console(
                    theme: AnsiConsoleTheme.Code,
                    outputTemplate:
                    "{ShortLevel} {Timestamp:HH:mm:ss} -- {Message:lj}{NewLine}{Exception}"
                ).MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Debug();
        }

        public static void PrintHeader(string text)
        {
            Log.Logger = GetLoggerConfiguration().CreateLogger();

            var titleLogger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}")
                .CreateLogger();
            var offset = ((Console.WindowWidth) / 2) + (text.Length / 2);
            var separator = new string('=', Console.WindowWidth);
            titleLogger.Information(separator);
            titleLogger.Information(string.Format("{0," + offset + "}", text));
            titleLogger.Information(separator);
        }
    }
}