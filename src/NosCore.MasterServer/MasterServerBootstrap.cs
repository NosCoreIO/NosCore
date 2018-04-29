using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace NosCore.MasterServer
{

    public static class MasterServerBootstrap
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
               .UseUrls("http://+:5000")
               .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Error))
               .PreferHostingUrls(true)
               .Build();
    }
    
}
