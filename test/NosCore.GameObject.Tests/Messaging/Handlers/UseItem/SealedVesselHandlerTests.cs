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
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem;

[TestClass]
public class SealedVesselHandlerTests
{
    private const short Vessel = 1945;
    private const short PinkJelly = 1386;
    private const short SladeGolem = 1405;

    private TestWorld World;
    private ClientSession _session;
    private MapInstance _map;
    private InventoryItemInstance _item;
    private SealedVesselHandler _handler;

    [TestInitialize]
    public async Task SetupAsync()
    {
        World = await TestWorld.CreateAsync();
        World.Inject(new List<NpcMonsterDto>
        {
            new() { NpcMonsterVNum = PinkJelly, MaxHp = 10000, Level = 70, CanWalk = true, IsHostile = true },
            new() { NpcMonsterVNum = SladeGolem, MaxHp = 10000, Level = 70, CanWalk = true, IsHostile = true },
        });
        World.Inject(new List<ItemSummonDto>
        {
            new() { ItemSummonId = 1, ItemVNum = Vessel, NpcMonsterVNum = PinkJelly },
            new() { ItemSummonId = 2, ItemVNum = Vessel, NpcMonsterVNum = SladeGolem },
        });
        _handler = World.Create<SealedVesselHandler>();
        _session = await World.CreateSessionAsync();
        _map = World.GetBaseMap(1);
        _session.Character.MapInstance = _map;
        _session.Character.PositionX = 5;
        _session.Character.PositionY = 7;
        _item = InventoryItemInstance.Create(new VesselInstance { Amount = 2 }, _session.Character.CharacterId);
        _item.Type = NoscorePocketType.Main;
        _item.Slot = 0;
        _session.Character.InventoryService[_item.ItemInstanceId] = _item;
    }

    [TestMethod]
    public async Task UsingAVesselReleasesOneOfItsMonstersWhereThePlayerStands()
    {
        World.Random.Setup(r => r.Next(0, 2)).Returns(1);

        await UseAsync();

        var released = _map.EnumerateMonsters().Single();
        Assert.AreEqual(SladeGolem, released.NpcMonster?.NpcMonsterVNum);
        Assert.AreEqual((short)5, released.PositionX);
        Assert.AreEqual((short)7, released.PositionY);
        Assert.AreEqual((short)1, _item.ItemInstance.Amount);
        Assert.IsTrue(_session.LastPackets.OfType<IvnPacket>().Any());
    }

    [TestMethod]
    public async Task TheLastVesselIsUsedUp()
    {
        _item.ItemInstance.Amount = 1;

        await UseAsync();

        Assert.AreEqual(1, _map.EnumerateMonsters().Count());
        Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType(_item.Slot, _item.Type));
    }

    [TestMethod]
    public async Task InGlacernonTheVesselIsRefusedAndKept()
    {
        _map.IsAct4 = true;

        await UseAsync();

        Assert.IsFalse(_map.EnumerateMonsters().Any());
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
        var refusal = _session.LastPackets.OfType<SayiPacket>().Single();
        Assert.AreEqual(Game18NConstString.CanNotBeUsedHere, refusal.Message);
        Assert.AreEqual(SayColorType.Yellow, refusal.Type);
    }

    [TestMethod]
    public async Task AnItemWithNoMonsterListedIsKept()
    {
        _item.ItemInstance.Item.VNum = 9999;
        _item.ItemInstance.ItemVNum = 9999;

        await UseAsync();

        Assert.IsFalse(_map.EnumerateMonsters().Any());
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
    }

    [TestMethod]
    public async Task AnotherEffectIsIgnored()
    {
        _item.ItemInstance.Item.Effect = ItemEffectType.Teleport;

        await UseAsync();

        Assert.IsFalse(_map.EnumerateMonsters().Any());
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
    }

    private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _item,
        new UseItemPacket { Type = PocketType.Main, Slot = 0 }));

    private sealed class VesselInstance : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
    {
        public new Guid Id { get; set; } = Guid.NewGuid();
        public new short ItemVNum { get; set; } = Vessel;
        public Item Item { get; set; } = new()
        {
            VNum = Vessel, Type = NoscorePocketType.Main, Effect = ItemEffectType.SealedVessel, EffectValue = 1
        };
        public object Clone() => MemberwiseClone();
    }
}
