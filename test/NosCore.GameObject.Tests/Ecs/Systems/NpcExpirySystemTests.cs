//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Systems;

[TestClass]
public class NpcExpirySystemTests
{
    private TestWorld World;
    private MapInstance _map;
    private readonly NpcExpirySystem _system = new();
    private readonly NpcMonsterDto _campfire = new() { NpcMonsterVNum = 956, MaxHp = 900 };

    [TestInitialize]
    public async Task SetupAsync()
    {
        World = await TestWorld.CreateAsync();
        _map = World.GetBaseMap(1);
    }

    [TestMethod]
    public void AnNpcIsKeptUntilItsTimeIsUp()
    {
        PutUpFor(Duration.FromMinutes(3));
        World.Clock.Advance(Duration.FromMinutes(2));

        Assert.AreEqual(0, TakeExpired().Count);
    }

    [TestMethod]
    public void AnNpcWhoseTimeIsUpIsTaken()
    {
        var npc = PutUpFor(Duration.FromMinutes(3));
        World.Clock.Advance(Duration.FromMinutes(3));

        CollectionAssert.AreEqual(new List<Entity> { npc.Entity }, TakeExpired());
    }

    [TestMethod]
    public void AnNpcWithoutATimeIsLeftAlone()
    {
        _map.PlaceNpc(_campfire, 3, 4);
        World.Clock.Advance(Duration.FromDays(1));

        Assert.AreEqual(0, TakeExpired().Count);
    }

    private NpcComponentBundle PutUpFor(Duration lifetime)
    {
        var npc = _map.PlaceNpc(_campfire, 3, 4);
        npc.World.AddComponent(npc.Entity, new ExpiryComponent(World.Clock.GetCurrentInstant() + lifetime));
        return npc;
    }

    private List<Entity> TakeExpired()
    {
        var expired = new List<Entity>();
        _system.TakeExpired(_map.EcsWorld, World.Clock.GetCurrentInstant(), expired);
        return expired;
    }
}
