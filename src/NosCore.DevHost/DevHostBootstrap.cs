//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NosCore.Shared.Configuration;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NosCore.DevHost
{
    public static class DevHostBootstrap
    {
        private const string ConsoleText = "DEV HOST - NosCoreIO";

        public static async Task Main(string[] args)
        {
            // The servers look for configuration/ relative to the folder they are built into.
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            ConfiguratorBuilder.InitializeConfiguration(args, ["logger.yml"]);
            Logger.GetLoggerConfiguration().CreateLogger();
            Logger.PrintHeader(ConsoleText);

            await new HostBuilder()
                .UseSerilog()
                .UseConsoleLifetime()
                .ConfigureServices(services =>
                {
                    services.AddI18NLogs();
                    services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
                    services.Configure<ConsoleLifetimeOptions>(o => o.SuppressStatusMessages = true);
                    // The world server saves every character on its way out.
                    services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromMinutes(2));
                    services.AddHostedService<ServerProcesses>();
                })
                .Build()
                .RunAsync();
        }
    }
}
