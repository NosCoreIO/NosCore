//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using NosCore.Core;
using NosCore.Core.Services.IdService;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Generated;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MateService;
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
        services.AddSingleton<IIdService<Mate>>(_ => new IdService<Mate>(2000000));

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
        services.AddSingleton<IMateService, MateService>();
        services.AddSingleton<IFamilyService, FamilyService>();

        // NosCore.DiGenerator emits these registrations at compile time. Lifetime still comes
        // from ISingletonService; a marked class matching no suffix now fails with NOSDI001.
        services.AddGeneratedGameObjectServices();
    }
}
