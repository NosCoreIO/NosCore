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
        // Local in-process message bus. Handlers are auto-discovered by convention
        // (any class with a Handle/HandleAsync method whose first parameter is the
        // message type) from ApplicationAssembly + any extra handler assemblies.
        //
        // Pinning ApplicationAssembly scopes Wolverine's default scan so it doesn't
        // sweep every loaded native runtime DLL looking for IWolverineExtension impls
        // — that's what produced the "To disable automatic Wolverine extension finding"
        // startup notice. Handler convention stays enabled so new *Handler classes are
        // picked up without needing an explicit registration.
        public static IHostBuilder UseNosCoreWolverine(this IHostBuilder builder, string serviceName,
            params Assembly[] handlerAssemblies)
        {
            return builder.UseWolverine(opts =>
            {
                opts.ServiceName = serviceName;
                opts.ApplicationAssembly = typeof(WolverineHostExtensions).Assembly;

                // From Wolverine 6 on, the runtime compiler lives in a separate package
                // (WolverineFx.RuntimeCompilation) which normally registers itself as an
                // extension. Referencing it is not enough here: ExtensionDiscovery.ManualOnly
                // below turns extension discovery off, so it has to be invoked by hand.
                //
                // Without this the server builds clean and then dies on startup with "no
                // IAssemblyGenerator (Roslyn) is registered" - handlers are generated on first
                // run and there is nobody left to compile them. No build can catch it.
                opts.UseRuntimeCompilation();
                foreach (var asm in handlerAssemblies)
                {
                    opts.Discovery.IncludeAssembly(asm);
                }
            }, ExtensionDiscovery.ManualOnly);
        }
    }
}
