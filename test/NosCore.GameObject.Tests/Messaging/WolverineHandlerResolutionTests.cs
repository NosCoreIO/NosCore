//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Algorithm.ExperienceService;
using NosCore.Database.Hosting;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Services.InventoryService;

namespace NosCore.GameObject.Tests.Messaging
{
    // Regression test for the class of bug where a Wolverine handler is registered but a
    // transitive dependency (registered only with Autofac) is invisible to Wolverine's
    // IServiceCollection-based code-gen planner. When that happens production crashes at
    // host build time with "does not have a suitable, public constructor for Wolverine or
    // is missing registered dependencies".
    //
    // We mirror the production wiring (PersistenceModule.MirrorTo +
    // WolverineDependencyRegistrar.RegisterDependencies + the *Service scan), then for
    // every handler under Messaging/Handlers we walk its public constructor and assert
    // each parameter type is registered. We do NOT actually instantiate handlers — that
    // would require a live DB — but the registration check catches the same failure mode
    // as Wolverine's planner.
    [TestClass]
    public class WolverineHandlerResolutionTests
    {
        [TestMethod]
        public void EveryWolverineHandlerHasItsConstructorDepsRegistered()
        {
            var services = BuildProductionLikeServiceCollection();
            var registered = new HashSet<Type>(services.Select(d => d.ServiceType));

            // Add the framework-supplied types Wolverine + ASP.NET inject into handlers.
            // These come from the host platform, not our wiring.
            registered.Add(typeof(Wolverine.IMessageBus));
            registered.Add(typeof(Wolverine.IMessageContext));
            foreach (var openGeneric in new[]
            {
                typeof(Microsoft.Extensions.Logging.ILogger<>),
                typeof(Microsoft.Extensions.Options.IOptions<>),
                typeof(NosCore.Shared.I18N.ILogLanguageLocalizer<>),
            })
            {
                registered.Add(openGeneric);
            }

            var handlerTypes = typeof(WolverineDependencyRegistrar).Assembly.GetTypes()
                .Where(t => t.Namespace?.Contains(".Messaging.Handlers.") == true
                    && t.IsClass && !t.IsAbstract && t.IsSealed)
                .ToList();

            Assert.IsTrue(handlerTypes.Count > 0, "Did not discover any Wolverine handler types");

            var missing = new List<string>();
            foreach (var handler in handlerTypes)
            {
                var ctor = handler.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
                foreach (var param in ctor.GetParameters())
                {
                    if (!IsKnown(registered, param.ParameterType))
                    {
                        missing.Add($"{handler.FullName}.ctor({param.Name}: {param.ParameterType.FullName})");
                    }
                }
            }

            Assert.AreEqual(0, missing.Count,
                "Handlers reference deps not registered in IServiceCollection — Wolverine code-gen will fail at host build time:\n  "
                + string.Join("\n  ", missing));
        }

        private static bool IsKnown(HashSet<Type> registered, Type t)
        {
            if (registered.Contains(t))
            {
                return true;
            }
            if (t.IsGenericType)
            {
                if (registered.Contains(t.GetGenericTypeDefinition()))
                {
                    return true;
                }
                if (t.GetGenericTypeDefinition() == typeof(IEnumerable<>) && registered.Contains(t.GetGenericArguments()[0]))
                {
                    return true;
                }
            }
            return false;
        }

        private static IServiceCollection BuildProductionLikeServiceCollection()
        {
            var services = new ServiceCollection();

            // Mirrors WorldServerBootstrap.ConfigureServices ordering.
            services.AddSingleton<Serilog.ILogger>(_ => Serilog.Log.Logger);
            services.AddSingleton<IClock>(_ => SystemClock.Instance);
            services.AddTransient<NosCore.Core.I18N.IGameLanguageLocalizer, NosCore.Core.I18N.GameLanguageLocalizer>();

            foreach (var implType in new[] { typeof(IInventoryService).Assembly, typeof(IExperienceService).Assembly }
                .SelectMany(a => a.GetTypes())
                .Where(t => t.Name.EndsWith("Service") && t.IsClass && !t.IsAbstract))
            {
                foreach (var iface in implType.GetInterfaces())
                {
                    services.AddTransient(iface, implType);
                }
                services.AddTransient(implType);
            }

            PersistenceModule.MirrorTo(services);
            WolverineDependencyRegistrar.RegisterDependencies(services);

            return services;
        }
    }
}
