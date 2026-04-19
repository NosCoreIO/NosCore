//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;

namespace NosCore.GameObject.Messaging;

// Mirrors the Autofac registrations Wolverine handlers depend on into IServiceCollection.
//
// Why: Wolverine's code generation inspects IServiceCollection at host-build time to plan
// how each handler's constructor will be invoked. Types only registered with Autofac are
// invisible to that planner, even though the runtime IServiceProvider (Autofac-backed via
// AutofacServiceProviderFactory) can resolve them.
//
// Scope: every GameObject-side dep that handlers reach via service composition. The
// DAO/DbContext side is mirrored separately by NosCore.Database.Hosting.PersistenceModule.MirrorTo
// (kept distinct so this assembly doesn't have to reference NosCore.Database).
//
// Bootstrap and tests both call this so they cannot drift.
public static class WolverineDependencyRegistrar
{
    public static void RegisterDependencies(IServiceCollection services)
    {
        services.AddSingleton<ISessionRegistry, SessionRegistry>();
        services.AddSingleton<IMinilandRegistry, MinilandRegistry>();
        services.AddSingleton<IMapInstanceRegistry, MapInstanceRegistry>();
        services.AddSingleton<IExchangeRequestRegistry, ExchangeRequestRegistry>();

        foreach (var hubType in typeof(ChannelHubClient).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("HubClient") && t.IsClass && !t.IsAbstract))
        {
            foreach (var iface in hubType.GetInterfaces())
            {
                services.AddSingleton(iface, hubType);
            }
            services.AddSingleton(hubType);
        }
    }
}
