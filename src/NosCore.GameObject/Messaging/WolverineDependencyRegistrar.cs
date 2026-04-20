//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NosCore.Core;
using NosCore.Core.Services.IdService;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.PathFinder.Heuristic;
using NosCore.PathFinder.Interfaces;

namespace NosCore.GameObject.Messaging;

// Single source of truth for every GameObject-side registration that both MSDI
// (for Wolverine codegen) and Autofac (for runtime resolution) need to see.
//
// AutofacServiceProviderFactory.Populate() copies MSDI registrations into the
// Autofac container at host-build time, so anything registered here becomes
// visible to both: Wolverine at codegen, Autofac at runtime. Registering a
// service here and again on the Autofac side duplicates the registration and
// is the most common cause of "An item with the same key..." style drift
// bugs — so bootstrap and tests both call this and nowhere else.
//
// DAO/DbContext side is mirrored separately by PersistenceModule.MirrorTo so
// this assembly doesn't have to reference NosCore.Database.
public static class WolverineDependencyRegistrar
{
    public static void RegisterDependencies(IServiceCollection services)
    {
        // ID generators — one per scoped entity family. Seed values come from
        // production behaviour: map items start at 100k to avoid colliding with
        // monster/npc visual ids that live below that.
        services.AddSingleton<IIdService<Group>>(_ => new IdService<Group>(1));
        services.AddSingleton<IIdService<MapItemComponentBundle>>(_ => new IdService<MapItemComponentBundle>(100000));
        services.AddSingleton<IIdService<ChannelInfo>>(_ => new IdService<ChannelInfo>(1));

        // Pathfinder / heuristic — OctileDistance is the standard NosTale grid
        // metric (diagonal moves cost sqrt(2), orthogonal cost 1).
        services.AddSingleton<IHeuristic, OctileDistanceHeuristic>();

        // Session / map registries. Singletons because they own global state
        // keyed by channel id and map instance id respectively.
        services.AddSingleton<ISessionRegistry, SessionRegistry>();
        services.AddSingleton<IMinilandRegistry, MinilandRegistry>();
        services.AddSingleton<IMapInstanceRegistry, MapInstanceRegistry>();
        services.AddSingleton<IExchangeRequestRegistry, ExchangeRequestRegistry>();
        services.AddSingleton<ISessionGroupFactory, SessionGroupFactory>();

        // Inter-channel hub clients — one instance per concrete HubClient, each
        // exposed as all of its implemented interfaces so features that depend on
        // a specific hub contract (IFriendHub, IBlacklistHub, …) resolve cleanly.
        foreach (var hubType in typeof(ChannelHubClient).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("HubClient") && t.IsClass && !t.IsAbstract))
        {
            foreach (var iface in hubType.GetInterfaces())
            {
                services.AddSingleton(iface, hubType);
            }
            services.AddSingleton(hubType);
        }

        // Convention-based scan for the rest of the game-object services. Naming
        // tells us WHAT a class is, not HOW to resolve it — so lifetime comes from
        // the ISingletonService marker interface (implemented by classes that own
        // shared state: caches, queues, per-entity maps). Everything else is
        // transient so short-lived handlers don't accidentally share mutable state.
        //
        // Matched suffixes cover the vocabulary we actually use across the codebase:
        // *Service, *Provider, *Resolver, *Calculator, *Catalog, *Queue, *Ai.
        // New classes can add a suffix here if they want auto-discovery, or they
        // can be registered explicitly above.
        var gameObjectAssembly = typeof(WolverineDependencyRegistrar).Assembly;

        foreach (var impl in gameObjectAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
            .Where(t => typeof(INrunEventHandler).IsAssignableFrom(t)))
        {
            services.AddTransient(typeof(INrunEventHandler), impl);
            services.AddTransient(impl);
        }

        var suffixes = new[] { "Service", "Provider", "Resolver", "Calculator", "Catalog", "Queue", "Ai" };
        foreach (var impl in gameObjectAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
            .Where(t => suffixes.Any(suffix => t.Name.EndsWith(suffix))))
        {
            var lifetime = typeof(ISingletonService).IsAssignableFrom(impl)
                ? ServiceLifetime.Singleton
                : ServiceLifetime.Transient;

            foreach (var iface in impl.GetInterfaces()
                .Where(i => i != typeof(ISingletonService) && !IsSystemInterface(i)))
            {
                services.Add(new ServiceDescriptor(iface, impl, lifetime));
            }
            services.Add(new ServiceDescriptor(impl, impl, lifetime));
        }
    }

    // Filter out framework interfaces (IDisposable, IAsyncDisposable, IEquatable, …)
    // so the scan only registers domain-facing contracts.
    private static bool IsSystemInterface(Type iface)
        => iface.Namespace?.StartsWith("System", StringComparison.Ordinal) == true;
}
