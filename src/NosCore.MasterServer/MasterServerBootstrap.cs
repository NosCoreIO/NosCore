//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NosCore.Shared.Configuration;
using Serilog;
using System;

namespace NosCore.MasterServer
{
    public static class MasterServerBootstrap
    {
        public static void Main()
        {
            try
            {
                BuildWebHost(Array.Empty<string>()).Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
                .UseConfiguration(ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "master.yml" }))
                .UseStartup<Startup>()
                .PreferHostingUrls(true)
                .SuppressStatusMessages(true)
                .Build();
        }
    }
}
