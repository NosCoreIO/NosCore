//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Infastructure;
using NosCore.PacketHandlers.Generated;
using NosCore.PacketHandlers.Login;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.PacketHandlers.Tests
{
    // LegacyScan is the startup scan the generator replaced, kept as the oracle its
    // output is diffed against.
    [TestClass]
    public class PacketHandlerRegistrationParityTests
    {
        [TestMethod]
        public void GeneratedWorldHandlerListMatchesTheReflectionScan()
            => AssertMatches(LegacyScan(typeof(IWorldPacketHandler)), GeneratedPacketHandlers.WorldHandlerTypes);

        [TestMethod]
        public void GeneratedLoginHandlerListMatchesTheReflectionScan()
            => AssertMatches(LegacyScan(typeof(ILoginPacketHandler)), GeneratedPacketHandlers.LoginHandlerTypes);

        // The old walk returned null - silently dropping the handler - when the chain
        // did not match.
        [TestMethod]
        public void EveryHandlerReportsThePacketItsBaseClassWasClosedOver()
        {
            var mismatched = GeneratedPacketHandlers.WorldHandlerTypes
                .Concat(GeneratedPacketHandlers.LoginHandlerTypes)
                .Select(t => new
                {
                    Type = t,
                    Declared = (Type?)t.GetProperty(nameof(IPacketHandler.PacketType))!
                        .GetValue(System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(t)),
                    FromBase = t.BaseType?.GenericTypeArguments.FirstOrDefault()
                })
                .Where(x => x.Declared != x.FromBase)
                .Select(x => $"{x.Type.Name}: PacketType={x.Declared?.Name}, base={x.FromBase?.Name}")
                .ToList();

            Assert.AreEqual(0, mismatched.Count, string.Join(Environment.NewLine, mismatched));
        }

        private static void AssertMatches(IEnumerable<Type> legacy, IEnumerable<Type> generated)
        {
            var expected = legacy.OrderBy(t => t.FullName, StringComparer.Ordinal).ToList();
            var actual = generated.OrderBy(t => t.FullName, StringComparer.Ordinal).ToList();

            var missing = expected.Except(actual).Select(t => $"MISSING: {t.FullName}");
            var extra = actual.Except(expected).Select(t => $"EXTRA:   {t.FullName}");
            var message = string.Join(Environment.NewLine, missing.Concat(extra));

            Assert.AreEqual(string.Empty, message, message);
        }

        private static IEnumerable<Type> LegacyScan(Type marker) =>
            typeof(NoS0575PacketHandler).Assembly.GetTypes()
                .Where(type => typeof(IPacketHandler).IsAssignableFrom(type) && marker.IsAssignableFrom(type))
                // The old scan passed GetTypes() straight to Autofac, so it saw interfaces and
                // abstract bases too; only concrete classes were ever resolvable.
                .Where(type => type is { IsClass: true, IsAbstract: false });
    }
}
