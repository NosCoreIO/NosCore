//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Parser.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Parser.Tests
{
    // The DAO registrations at startup derive each TPk from the entity model, so a
    // parser declaring IDao<TDto, TPk> with a stale key type compiles but fails DI at
    // runtime. This walks every parser constructor and checks the declared key against
    // the model, which turns that startup crash into a test failure.
    [TestClass]
    public class ParserDaoContractTests
    {
        [TestMethod]
        public void EveryParserDaoParameterMatchesTheEntityPrimaryKey()
        {
            var entities = typeof(Account).Assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsPublic: true })
                .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
            using var context = new NosCoreContext(new DbContextOptionsBuilder<NosCoreContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var model = context.Model;

            var mismatches = new List<string>();
            foreach (var parser in typeof(CardParser).Assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericType: false } && t.Name.EndsWith("Parser")))
            {
                foreach (var parameter in parser.GetConstructors().SelectMany(c => c.GetParameters()))
                {
                    var parameterType = parameter.ParameterType;
                    if (!parameterType.IsGenericType || parameterType.GetGenericTypeDefinition() != typeof(IDao<,>))
                    {
                        continue;
                    }

                    var dtoType = parameterType.GetGenericArguments()[0];
                    var declaredKeyType = parameterType.GetGenericArguments()[1];
                    if (typeof(IItemInstanceDto).IsAssignableFrom(dtoType))
                    {
                        continue;
                    }

                    var entityName = dtoType.Name.EndsWith("Dto")
                        ? dtoType.Name[..^3]
                        : dtoType.Name;
                    if (!entities.TryGetValue(entityName, out var entityType))
                    {
                        mismatches.Add($"{parser.Name}: no entity found for {dtoType.Name}");
                        continue;
                    }

                    var keyProperties = model.FindEntityType(entityType)?.FindPrimaryKey()?.Properties;
                    if (keyProperties == null || keyProperties.Count != 1)
                    {
                        continue;
                    }

                    var modelKeyType = keyProperties[0].ClrType;
                    if (modelKeyType != declaredKeyType)
                    {
                        mismatches.Add(
                            $"{parser.Name} declares IDao<{dtoType.Name}, {declaredKeyType.Name}> but {entityName}.{keyProperties[0].Name} is {modelKeyType.Name}");
                    }
                }
            }

            Assert.IsEmpty(mismatches, string.Join(Environment.NewLine, mismatches));
        }
    }
}
