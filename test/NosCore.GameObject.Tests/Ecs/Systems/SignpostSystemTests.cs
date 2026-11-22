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
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Systems;

[TestClass]
public class SignpostSystemTests
{
    private TestWorld World;
    private MapInstance _map;
    private readonly SignpostSystem _system = new();
    private readonly NpcMonsterDto _signpost = new() { NpcMonsterVNum = 921, MaxHp = 18000 };

    [TestInitialize]
    public async Task SetupAsync()
    {
        World = await TestWorld.CreateAsync();
        _map = World.GetBaseMap(1);
    }

    [TestMethod]
    public void ASignpostShowsTheShareOfItsTimeItHasLeft()
    {
        var signpost = PutUp();
        World.Clock.Advance(Duration.FromMinutes(45));

        Assert.AreEqual(0, TakeExpired().Count);
        Assert.AreEqual(4500, signpost.Hp);
        Assert.AreEqual(25, signpost.GenerateIn().InNonPlayerSubPacket?.InAliveSubPacket?.Hp);
    }

    [TestMethod]
    public void ASignpostWhoseTimeIsUpIsTakenDown()
    {
        var signpost = PutUp();
        World.Clock.Advance(SignpostSystem.Lifetime);

        CollectionAssert.AreEqual(new List<Entity> { signpost.Entity }, TakeExpired());
    }

    [TestMethod]
    public void AnyOtherNpcIsLeftAlone()
    {
        var npc = _map.PlaceNpc(_signpost, 3, 4);
        World.Clock.Advance(SignpostSystem.Lifetime);

        Assert.AreEqual(0, TakeExpired().Count);
        Assert.AreEqual(18000, npc.Hp);
    }

    private NpcComponentBundle PutUp()
    {
        var signpost = _map.PlaceNpc(_signpost, 3, 4);
        signpost.World.SetComponent(signpost.Entity,
            new SignpostComponent(7, "Owner", World.Clock.GetCurrentInstant() + SignpostSystem.Lifetime));
        return signpost;
    }

    private List<Entity> TakeExpired()
    {
        var expired = new List<Entity>();
        _system.TakeExpired(_map.EcsWorld, World.Clock.GetCurrentInstant(), expired);
        return expired;
    }
}
