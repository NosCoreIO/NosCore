using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace NosCore.WorldServer
{
    public static class WorldServerBootstrap
    {
        public static void Main()
        {
            var host = BuildWebHost(null);
            host.Start();
            host.WaitForShutdown();
        }
        private static IWebHost BuildWebHost(string[] args) =>
           WebHost.CreateDefaultBuilder(args)
               .UseStartup<Startup>()
               .UseUrls("http://localhost:5001")
               .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Error))
               .PreferHostingUrls(true)
               .Build();
    }
}