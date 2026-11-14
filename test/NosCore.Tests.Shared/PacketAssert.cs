//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Packets;
using NosCore.Packets.Attributes;

namespace NosCore.Tests.Shared
{
    // Compares packets by the values of their fields, so game tests do not depend on the wire format,
    // which NosCore.Packets owns and tests.
    public static class PacketAssert
    {
        public static void AreEqual<T>(T expected, T actual) where T : class
        {
            var differences = new List<string>();
            Compare(expected, actual, typeof(T).Name, differences);
            if (differences.Count > 0)
            {
                throw new AssertFailedException(
                    $"The packets differ:{Environment.NewLine}{string.Join(Environment.NewLine, differences)}");
            }
        }

        private static void Compare(object? expected, object? actual, string path, List<string> differences)
        {
            if (expected is null || actual is null)
            {
                if (!ReferenceEquals(expected, actual))
                {
                    differences.Add($"{path}: expected {Describe(expected)}, was {Describe(actual)}");
                }

                return;
            }

            var type = expected.GetType();
            if (type != actual.GetType())
            {
                differences.Add($"{path}: expected a {type.Name}, was a {actual.GetType().Name}");
                return;
            }

            if (IsLeaf(type))
            {
                if (!expected.Equals(actual))
                {
                    differences.Add($"{path}: expected {Describe(expected)}, was {Describe(actual)}");
                }

                return;
            }

            if (expected is IEnumerable expectedItems)
            {
                var left = expectedItems.Cast<object?>().ToList();
                var right = ((IEnumerable)actual).Cast<object?>().ToList();
                if (left.Count != right.Count)
                {
                    differences.Add($"{path}: expected {left.Count} entries, was {right.Count}");
                }

                for (var i = 0; i < Math.Min(left.Count, right.Count); i++)
                {
                    Compare(left[i], right[i], $"{path}[{i}]", differences);
                }

                return;
            }

            foreach (var property in Fields(type))
            {
                Compare(property.GetValue(expected), property.GetValue(actual), $"{path}.{property.Name}", differences);
            }
        }

        private static IEnumerable<PropertyInfo> Fields(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToList();
            var indexed = properties
                .Select(p => (Property: p, Index: p.GetCustomAttribute<PacketIndexAttribute>()?.Index))
                .Where(p => p.Index != null)
                .OrderBy(p => p.Index)
                .Select(p => p.Property)
                .ToList();
            if (indexed.Count > 0)
            {
                return indexed;
            }

            var packetBase = typeof(PacketBase).GetProperties().Select(p => p.Name).ToHashSet();
            return properties.Where(p => !packetBase.Contains(p.Name));
        }

        private static bool IsLeaf(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string)
                || underlying == typeof(decimal) || underlying == typeof(DateTime) || underlying == typeof(Guid)
                || underlying == typeof(TimeSpan);
        }

        private static string Describe(object? value) => value switch
        {
            null => "null",
            string text => $"\"{text}\"",
            _ => value.ToString() ?? value.GetType().Name,
        };
    }
}
