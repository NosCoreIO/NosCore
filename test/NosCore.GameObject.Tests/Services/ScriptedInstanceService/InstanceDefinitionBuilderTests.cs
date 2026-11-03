//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.ScriptedInstanceService;
using System.Linq;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class InstanceDefinitionBuilderTests
    {
        [TestMethod]
        public void AnInstanceReadsAsOneSentencePerFact()
        {
            var definition = InstanceDefinitionBuilder
                .Named(id: 3, label: "Cuby", title: "Mother Cuby")
                .ForLevels(20, 45)
                .WithLives(3)
                .StartingAt(12, 34)
                .Rewarding(gold: 15000, reputation: 200)
                .WithRoom(2004, out var entrance)
                .WithRoom(2005, out var lair, indexX: 1)
                .Requiring(1000, 2)
                .Drawing(1012, 3, design: 7, randomRare: true)
                .WithSpecialReward(2282, 1, heroic: true)
                .WithReward(1030, 5)
                .Build();

            Assert.AreEqual("Cuby", definition.Label);
            Assert.AreEqual(20, definition.LevelMinimum);
            Assert.AreEqual(15000L, definition.Gold);
            Assert.AreEqual(2, definition.Rooms.Count);
            Assert.AreEqual(1000, definition.RequiredItems.Single().VNum);
            Assert.IsTrue(definition.SpecialItems.Single().IsHeroic);

            // Keys are handed out, not written down: the compiler carries what the XML did not.
            Assert.AreNotEqual(entrance, lair);
            Assert.AreEqual(2004, definition.Rooms.Single(s => s.Key == entrance).VNum);
            Assert.AreEqual(2005, definition.Rooms.Single(s => s.Key == lair).VNum);
        }

        [TestMethod]
        public void AnInstanceThatGivesNothingIsStillAnInstance()
        {
            var definition = InstanceDefinitionBuilder.Named(1, "Empty", "Empty")
                .WithRoom(2004, out _)
                .Build();

            Assert.AreEqual(0, definition.DrawItems.Count);
            Assert.AreEqual(0, definition.Gold);
            Assert.AreEqual(1, definition.Rooms.Count);
        }

        [TestMethod]
        public void TheRoomsKeepTheOrderTheyWereWrittenIn()
        {
            // The first room is where the party lands, so order is the author's.
            var definition = InstanceDefinitionBuilder.Named(1, "Three", "Three")
                .WithRoom(30, out _)
                .WithRoom(10, out _)
                .WithRoom(20, out _)
                .Build();

            CollectionAssert.AreEqual(new short[] { 30, 10, 20 },
                definition.Rooms.Select(s => s.VNum).ToArray());
        }

        [TestMethod]
        public void ADefinitionDoesNotGrowWhenTheBuilderIsUsedAgain()
        {
            var builder = InstanceDefinitionBuilder.Named(1, "First", "First")
                .WithRoom(2004, out _)
                .WithReward(1030, 5);

            var first = builder.Build();

            builder.WithRoom(2005, out _).WithReward(1031, 1);
            var second = builder.Build();

            Assert.AreEqual(1, first.Rooms.Count);
            Assert.AreEqual(1, first.GiftItems.Count);
            Assert.AreEqual(2, second.Rooms.Count);
            Assert.AreEqual(2, second.GiftItems.Count);
        }
    }
}
