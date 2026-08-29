//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data;
using NosCore.Data.StaticEntities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NosCore.Database.Tests
{
    [TestClass]
    public class MemberAccessorTests
    {
        private sealed class Target
        {
            public long Number { get; set; }
            public string? Text { get; set; }
            public short? Nullable { get; set; }
            internal List<string>? Hidden { get; set; }
            public long ReadOnly { get; } = 7;
            public string Field = "field";
            public string this[int index] => index.ToString();
        }

        [TestMethod]
        public void APublicValueRoundTrips()
        {
            var accessor = MemberAccessor.For(typeof(Target));
            var target = new Target();

            accessor[target, "Number"] = 42L;

            Assert.AreEqual(42L, target.Number);
            Assert.AreEqual(42L, accessor[target, "Number"]);
        }

        [TestMethod]
        public void AReferenceAndANullRoundTrip()
        {
            var accessor = MemberAccessor.For(typeof(Target));
            var target = new Target { Text = "before" };

            accessor[target, "Text"] = "after";
            Assert.AreEqual("after", accessor[target, "Text"]);

            accessor[target, "Text"] = null;
            Assert.IsNull(target.Text);
            Assert.IsNull(accessor[target, "Text"]);
        }

        [TestMethod]
        public void ANullableValueTypeTakesBothAValueAndNull()
        {
            var accessor = MemberAccessor.For(typeof(Target));
            var target = new Target();

            accessor[target, "Nullable"] = (short)5;
            Assert.AreEqual((short)5, target.Nullable);

            accessor[target, "Nullable"] = null;
            Assert.IsNull(target.Nullable);
        }

        // The reason this exists rather than an expression tree: a compiled lambda cannot
        // reach a non-public member, and the parsers write six internal navigation properties.
        [TestMethod]
        public void AnInternalMemberIsWritable()
        {
            var accessor = MemberAccessor.For(typeof(Target));
            var target = new Target();

            accessor[target, "Hidden"] = new List<string> { "a", "b" };

            Assert.AreEqual(2, target.Hidden!.Count);
            Assert.AreSame(target.Hidden, accessor[target, "Hidden"]);
        }

        [TestMethod]
        public void TheInternalNavigationPropertiesTheParsersWriteAreAllWritable()
        {
            foreach (var (type, member) in new (Type, string)[]
                     {
                         (typeof(SkillDto), "BCards"), (typeof(SkillDto), "Combo"),
                         (typeof(SkillDto), "NpcMonsterSkill"), (typeof(ItemDto), "BCards"),
                         (typeof(ItemDto), "Drop"), (typeof(NpcMonsterDto), "Drop"),
                         (typeof(CardDto), "BCards"), (typeof(QuestDto), "QuestObjective")
                     })
            {
                var setter = type.GetProperty(member,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetSetMethod(true)!;
                Assert.IsFalse(setter.IsPublic, $"{type.Name}.{member} is expected to be non-public");

                var instance = Activator.CreateInstance(type)!;
                MemberAccessor.For(type)[instance, member] = null;
            }
        }

        [TestMethod]
        public void AFieldRoundTrips()
        {
            var accessor = MemberAccessor.For(typeof(Target));
            var target = new Target();

            accessor[target, "Field"] = "changed";

            Assert.AreEqual("changed", target.Field);
            Assert.AreEqual("changed", accessor[target, "Field"]);
        }

        [TestMethod]
        public void AGetOnlyPropertyReadsButDoesNotWrite()
        {
            var accessor = MemberAccessor.For(typeof(Target));
            var target = new Target();

            Assert.AreEqual(7L, accessor[target, "ReadOnly"]);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => accessor[target, "ReadOnly"] = 9L);
        }

        [TestMethod]
        public void AnUnknownNameSaysSoOnBothSides()
        {
            var accessor = MemberAccessor.For(typeof(Target));
            var target = new Target();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = accessor[target, "Nope"]);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => accessor[target, "Nope"] = 1L);
        }

        [TestMethod]
        public void AnIndexerIsNotMistakenForAMember()
        {
            // Item[int] has index parameters and no name a caller would use; emitting a getter
            // for it would throw at delegate creation rather than at first use.
            var accessor = MemberAccessor.For(typeof(Target));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = accessor[new Target(), "Item"]);
        }

        [TestMethod]
        public void TheAccessorForATypeIsBuiltOnce()
        {
            Assert.AreSame(MemberAccessor.For(typeof(Target)), MemberAccessor.For(typeof(Target)));
        }
    }
}
