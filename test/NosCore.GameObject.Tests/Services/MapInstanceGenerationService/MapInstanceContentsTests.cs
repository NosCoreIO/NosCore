//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.MapInstanceGenerationService
{
    [TestClass]
    public class MapInstanceContentsTests
    {
        private TestWorld World;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
        }

        private MapInstance MapWithTwoMonsters()
        {
            var map = new MapInstance(new GameObject.Map.Map
                {
                    MapId = 0,
                    NameI18NKey = "testMap",
                    Data = new byte[] { 2, 0, 2, 0, 0, 0, 0, 0 },
                },
                Guid.NewGuid(), false, MapInstanceType.NormalInstance, World.MapItemProvider.NotNull(),
                NullLogger<MapInstance>.Instance, World.Clock, World.MapChangeService,
                World.SessionGroupFactory, World.SessionRegistry,
                World.DistanceCalculator);
            map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 1, VNum = 1 }, new() { MapMonsterId = 2, VNum = 1 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, MaxHp = 10 } });
            return map;
        }

        private static List<long> MonstersShown(MapInstance map) =>
            map.GetMapItems(RegionType.EN).OfType<InPacket>()
                .Where(p => p.VisualType == VisualType.Monster)
                .Select(p => p.VisualId)
                .ToList();

        [TestMethod]
        public void AnArrivingPlayerIsShownEveryLivingMonster()
        {
            var map = MapWithTwoMonsters();

            CollectionAssert.AreEquivalent(new List<long> { 1, 2 }, MonstersShown(map));
        }

        [TestMethod]
        public void AnArrivingPlayerIsNotShownAMonsterWaitingToRespawn()
        {
            var map = MapWithTwoMonsters();
            var dead = map.GetMonsterById(1) ?? throw new InvalidOperationException();
            dead.Hp = 0;
            dead.IsAlive = false;

            CollectionAssert.AreEquivalent(new List<long> { 2 }, MonstersShown(map));
        }
    }
}
