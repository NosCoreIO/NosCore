//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem;

[TestClass]
public class CookingStationHandlerTests
{
    private const short SmallCampfireWood = 1375;
    private const short SmallCampfire = 956;

    private TestWorld World;
    private ClientSession _session;
    private MapInstance _map;
    private InventoryItemInstance _item;
    private CookingStationHandler _handler;

    [TestInitialize]
    public async Task SetupAsync()
    {
        World = await TestWorld.CreateAsync();
        World.Inject(new List<NpcMonsterDto> { new() { NpcMonsterVNum = SmallCampfire, MaxHp = 900 } });
        _handler = World.Create<CookingStationHandler>();
        _session = await World.CreateSessionAsync();
        _map = World.GetBaseMap(1);
        _session.Character.MapInstance = _map;
        _session.Character.PositionX = 5;
        _session.Character.PositionY = 7;
        _item = InventoryItemInstance.Create(new KitInstance { Amount = 2 }, _session.Character.CharacterId);
        _item.Type = NoscorePocketType.Main;
        _item.Slot = 0;
        _session.Character.InventoryService[_item.ItemInstanceId] = _item;
    }

    [TestMethod]
    public async Task UsingAKitPutsItsNpcUpWhereThePlayerStandsForThreeMinutes()
    {
        await UseAsync();

        var station = _map.EnumerateNpcs().Single();
        Assert.AreEqual(SmallCampfire, station.NpcMonster?.NpcMonsterVNum);
        Assert.AreEqual((short)5, station.PositionX);
        Assert.AreEqual((short)7, station.PositionY);
        Assert.AreEqual(World.Clock.GetCurrentInstant() + CookingStationHandler.Lifetime,
            station.World.TryGetComponent<ExpiryComponent>(station.Entity)?.ExpiresAt);
        Assert.AreEqual((short)1, _item.ItemInstance.Amount);
        Assert.IsTrue(_session.LastPackets.OfType<IvnPacket>().Any());
        var shown = _map.LastPackets.OfType<InPacket>().Single();
        Assert.AreEqual(VisualType.Npc, shown.VisualType);
        Assert.AreEqual(SmallCampfire.ToString(), shown.VNum);
        Assert.AreEqual(station.VisualId, shown.VisualId);
    }

    [TestMethod]
    public async Task TheLastKitIsUsedUp()
    {
        _item.ItemInstance.Amount = 1;

        await UseAsync();

        Assert.AreEqual(1, _map.EnumerateNpcs().Count());
        Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType(_item.Slot, _item.Type));
    }

    [TestMethod]
    public async Task AKitWhoseNpcIsUnknownIsKept()
    {
        _item.ItemInstance.Item.EffectValue = 9999;

        await UseAsync();

        Assert.IsFalse(_map.EnumerateNpcs().Any());
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
    }

    [TestMethod]
    public async Task AnotherEffectIsIgnored()
    {
        _item.ItemInstance.Item.Effect = ItemEffectType.Teleport;

        await UseAsync();

        Assert.IsFalse(_map.EnumerateNpcs().Any());
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
    }

    private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _item,
        new UseItemPacket { Type = PocketType.Main, Slot = 0 }));

    private sealed class KitInstance : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
    {
        public new Guid Id { get; set; } = Guid.NewGuid();
        public new short ItemVNum { get; set; } = SmallCampfireWood;
        public Item Item { get; set; } = new()
        {
            VNum = SmallCampfireWood, Type = NoscorePocketType.Main, ItemType = ItemType.Special,
            Effect = ItemEffectType.CookingStation, EffectValue = SmallCampfire
        };
        public object Clone() => MemberwiseClone();
    }
}
