//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.GameObject.Ecs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NosCore.GameObject.Tests
{
    [TestClass]
    public class GameObjectMappingTests
    {
        private static IEnumerable<(Type Dto, Type GameObject)> Pairs() =>
            from dto in typeof(IStaticDto).Assembly.GetTypes()
            where typeof(IDto).IsAssignableFrom(dto) && dto.IsClass
            from gameObject in typeof(MapWorld).Assembly.GetTypes()
            where dto.IsAssignableFrom(gameObject)
            select (dto, gameObject);

        [TestMethod]
        public void GameObjectsBuiltByTheContainerHaveNoRequiredMembers()
        {
            var offenders = Pairs()
                .Where(p => GameObjectMapper.BuildsThroughContainer(p.GameObject))
                .Where(p => p.GameObject.IsDefined(typeof(RequiredMemberAttribute), true))
                .Select(p => p.GameObject.Name)
                .Distinct()
                .ToList();

            Assert.AreEqual(0, offenders.Count, string.Join(", ", offenders));
        }

        [TestMethod]
        public void EveryOtherGameObjectMapsFromItsDto()
        {
            var failures = new List<string>();
            foreach (var (dto, gameObject) in Pairs().Where(p => !GameObjectMapper.BuildsThroughContainer(p.GameObject)))
            {
                try
                {
                    RuntimeHelpers.GetUninitializedObject(dto).Adapt(dto, gameObject);
                }
                catch (Exception e)
                {
                    failures.Add($"{dto.Name} -> {gameObject.Name}: {e.Message}");
                }
            }

            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }
    }
}
