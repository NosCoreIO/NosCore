//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Generated;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Services.QuestService;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Tests
{
    // Guards the swap from runtime reflection scanning to NosCore.DiGenerator.
    //
    // LegacyScan below is a verbatim copy of what WolverineDependencyRegistrar used
    // to do at startup. It stays here, in the test project only, as the oracle the
    // generated registration list is diffed against. If a future change to the
    // generator drops or duplicates a service, this test fails with the exact delta.
    [TestClass]
    public class DependencyInjectionParityTests
    {
        private static readonly string[] ConventionSuffixes =
            { "Service", "Provider", "Resolver", "Calculator", "Catalog", "Queue", "Ai" };

        private readonly record struct Descriptor(string ServiceType, string ImplementationType, ServiceLifetime Lifetime);

        [TestMethod]
        public void GeneratedRegistrationsMatchTheReflectionScanTheyReplaced()
        {
            var legacy = LegacyScan().ToList();
            var generated = GeneratedServiceRegistrations.Descriptors
                .Select(d => new Descriptor(d.ServiceType, d.ImplementationType, d.Lifetime))
                .ToList();

            var missing = legacy.Except(generated).OrderBy(d => d.ServiceType).ToList();
            var extra = generated.Except(legacy).OrderBy(d => d.ServiceType).ToList();

            var message = string.Join(Environment.NewLine,
                missing.Select(d => $"MISSING from generated: {d.ServiceType} -> {d.ImplementationType} ({d.Lifetime})")
                    .Concat(extra.Select(d => $"EXTRA in generated:  {d.ServiceType} -> {d.ImplementationType} ({d.Lifetime})")));

            Assert.AreEqual(string.Empty, message, message);
        }

        // Where a contract has several implementations the last registration wins for
        // GetRequiredService, and the generator orders by name rather than by the
        // metadata order the old scan happened to produce. That reordering is only safe
        // for contracts nobody resolves singly, so the fan-in set is pinned here: the
        // two handler contracts are resolved as IEnumerable<T>, and IAsyncDisposable is
        // an artefact of the *HubClient scan applying no namespace filter (nothing
        // resolves it). A new entry in this list means some contract silently became
        // order-dependent and needs an explicit registration instead.
        [TestMethod]
        public void OnlyKnownContractsHaveMultipleImplementations()
        {
            var expected = new[]
            {
                "NosCore.GameObject.Messaging.Handlers.Nrun.INrunEventHandler",
                "NosCore.GameObject.Services.QuestService.IQuestTypeHandler",
                "System.IAsyncDisposable"
            };

            var actual = GeneratedServiceRegistrations.Descriptors
                .Where(d => d.ServiceType != d.ImplementationType)
                .GroupBy(d => d.ServiceType)
                .Where(g => g.Select(d => d.ImplementationType).Distinct().Count() > 1)
                .Select(g => g.Key)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();

            CollectionAssert.AreEqual(expected.OrderBy(s => s, StringComparer.Ordinal).ToList(), actual,
                $"fan-in contracts changed:{Environment.NewLine}{string.Join(Environment.NewLine, actual)}");
        }

        private static IEnumerable<Descriptor> LegacyScan()
        {
            var results = new List<Descriptor>();
            var gameObjectAssembly = typeof(MapWorld).Assembly;

            foreach (var hubType in typeof(ChannelHubClient).Assembly.GetTypes()
                .Where(t => t.Name.EndsWith("HubClient", StringComparison.Ordinal) && t.IsClass && !t.IsAbstract))
            {
                foreach (var iface in hubType.GetInterfaces())
                {
                    results.Add(new Descriptor(Name(iface), Name(hubType), ServiceLifetime.Singleton));
                }

                results.Add(new Descriptor(Name(hubType), Name(hubType), ServiceLifetime.Singleton));
            }

            foreach (var impl in gameObjectAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
                .Where(t => typeof(INrunEventHandler).IsAssignableFrom(t)))
            {
                results.Add(new Descriptor(Name(typeof(INrunEventHandler)), Name(impl), ServiceLifetime.Transient));
                results.Add(new Descriptor(Name(impl), Name(impl), ServiceLifetime.Transient));
            }

            foreach (var impl in gameObjectAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
                .Where(t => typeof(IQuestTypeHandler).IsAssignableFrom(t)))
            {
                results.Add(new Descriptor(Name(typeof(IQuestTypeHandler)), Name(impl), ServiceLifetime.Transient));
                results.Add(new Descriptor(Name(impl), Name(impl), ServiceLifetime.Transient));
            }

            foreach (var impl in gameObjectAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
                .Where(t => ConventionSuffixes.Any(suffix => t.Name.EndsWith(suffix, StringComparison.Ordinal))))
            {
                var lifetime = typeof(ISingletonService).IsAssignableFrom(impl)
                    ? ServiceLifetime.Singleton
                    : ServiceLifetime.Transient;

                foreach (var iface in impl.GetInterfaces()
                    .Where(i => i != typeof(ISingletonService) && !IsSystemInterface(i)))
                {
                    results.Add(new Descriptor(Name(iface), Name(impl), lifetime));
                }

                results.Add(new Descriptor(Name(impl), Name(impl), lifetime));
            }

            // The generator cannot emit a convention registration for an unbound generic
            // type, and MSDI would reject the descriptor anyway, so exclude them from the
            // oracle rather than pretending the old scan produced something usable.
            return results.Where(d => !d.ImplementationType.Contains('`'));
        }

        private static bool IsSystemInterface(Type iface)
            => iface.Namespace?.StartsWith("System", StringComparison.Ordinal) == true;

        // Formats a Type the way Roslyn's FullyQualifiedFormat does (minus global::),
        // so a constructed generic reads as Ns.IFoo<Ns.A, Ns.B> on both sides instead
        // of reflection's IFoo`2[[A, Asm, Version=...]] form.
        private static string Name(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName!.Replace('+', '.');
            }

            var definition = type.GetGenericTypeDefinition().FullName!.Replace('+', '.');
            var tick = definition.IndexOf('`');
            if (tick >= 0)
            {
                definition = definition.Substring(0, tick);
            }

            return $"{definition}<{string.Join(", ", type.GetGenericArguments().Select(Name))}>";
        }
    }
}
