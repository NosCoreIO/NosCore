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
    // LegacyScan is a verbatim copy of the startup scan the generator replaced, kept as
    // the oracle its output is diffed against.
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

            // Counted, not just matched: a duplicate gained or lost is still a difference.
            var legacyCounts = legacy.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
            var generatedCounts = generated.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());

            var message = string.Join(Environment.NewLine, legacyCounts.Keys.Union(generatedCounts.Keys)
                .Select(d => (Descriptor: d,
                    Legacy: legacyCounts.TryGetValue(d, out var l) ? l : 0,
                    Generated: generatedCounts.TryGetValue(d, out var g) ? g : 0))
                .Where(x => x.Legacy != x.Generated)
                .OrderBy(x => x.Descriptor.ServiceType)
                .Select(x =>
                    $"{x.Descriptor.ServiceType} -> {x.Descriptor.ImplementationType} ({x.Descriptor.Lifetime}): " +
                    $"scan {x.Legacy}, generated {x.Generated}"));

            Assert.AreEqual(string.Empty, message, message);
        }

        // The generator orders by name, so last-wins resolution only stays safe for contracts
        // nobody resolves singly. A new entry here means one silently became order-dependent.
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

            // MSDI rejects unbound generics, so the oracle excludes them too.
            return results.Where(d => !d.ImplementationType.Contains('`'));
        }

        private static bool IsSystemInterface(Type iface)
            => iface.Namespace?.StartsWith("System", StringComparison.Ordinal) == true;

        // Matches Roslyn's FullyQualifiedFormat so both sides read as Ns.IFoo<Ns.A>.
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
