//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Reflection;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace NosCore.GameObject.Messaging
{
    public static class WolverineHostExtensions
    {
        // Local in-process message bus. Handlers are auto-discovered by convention from this assembly
        // plus any extra handler assemblies passed in (any class with a Handle/HandleAsync method
        // whose first parameter is the message type).
        public static IHostBuilder UseNosCoreWolverine(this IHostBuilder builder, string serviceName,
            params Assembly[] handlerAssemblies)
        {
            return builder.UseWolverine(opts =>
            {
                opts.ServiceName = serviceName;
                opts.Discovery.IncludeAssembly(typeof(WolverineHostExtensions).Assembly);
                foreach (var asm in handlerAssemblies)
                {
                    opts.Discovery.IncludeAssembly(asm);
                }
            });
        }
    }
}
