//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpecLight;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NosCore.Database.Tests
{
    [TestClass]
    public class DatabaseTests
    {
        public static bool IsNullable(PropertyInfo property)
        {
            var nullable = property.CustomAttributes
                .FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
            if (nullable != null && nullable.ConstructorArguments.Count == 1)
            {
                var attributeArgument = nullable.ConstructorArguments[0];
                if (attributeArgument.ArgumentType == typeof(byte[]))
                {
                    var args = (ReadOnlyCollection<CustomAttributeTypedArgument>?)attributeArgument.Value;
                    if (args?.Count > 0 && args[0].ArgumentType == typeof(byte))
                    {
                        return (byte?)args[0].Value == 2;
                    }
                }
                else if (attributeArgument.ArgumentType == typeof(byte))
                {
                    return (byte?)attributeArgument.Value == 2;
                }
            }

            var context = property.DeclaringType?.CustomAttributes
                .FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");
            if (context != null &&
                context.ConstructorArguments.Count == 1 &&
                context.ConstructorArguments[0].ArgumentType == typeof(byte))
            {
                return (byte?)context.ConstructorArguments[0].Value == 2;
            }

            return false;
        }

        [TestMethod]
        public void AllNullableForeignKeysShouldBeAssociatedToNullableEntity()
        {
            new Spec("All nullable foreign keys should be associated to nullable entity")
                .Then(NullableForeignKeysShouldHaveNullableNavigationProperties)
                .Execute();
        }

        private void NullableForeignKeysShouldHaveNullableNavigationProperties()
        {
            using var ctx = new NosCoreContext(new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                Guid.NewGuid().ToString()).Options);
            var nonNullNavigation = new StringBuilder();
            foreach (var entityType in ctx.Model.GetEntityTypes())
            {
                foreach (var navigation in entityType.GetNavigations())
                {
                    var foreignKey = navigation.ForeignKey.Properties[0].PropertyInfo?.PropertyType;
                    if (navigation.IsCollection || navigation.PropertyInfo == null || foreignKey == null || !foreignKey.IsGenericType ||
                        foreignKey.GetGenericTypeDefinition() != typeof(Nullable<>))
                    {
                        continue;
                    }

                    if (!IsNullable(navigation.PropertyInfo))
                    {
                        nonNullNavigation.AppendLine(
                            $"{entityType.Name} -> {navigation.Name} is not nullable but it's foreign key is not required");
                    }
                }
            }

            Assert.AreEqual(string.Empty, nonNullNavigation.ToString());
        }
    }
}
