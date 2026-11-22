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
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem;

[TestClass]
public class SuctionFunnelHandlerTests
{
    private const short Funnel = 1173;
    private const short Bead = 1013;
    private const short KenkoSwordsman = 146;
    private const short Kovolt = 36;

    private TestWorld World;
    private ClientSession _session;
    private MapInstance _map;
    private InventoryItemInstance _item;
    private MonsterComponentBundle _monster;
    private Mock<IGameEventDispatcher> _gameEvents;
    private SuctionFunnelHandler _handler;

    [TestInitialize]
    public async Task SetupAsync()
    {
        World = await TestWorld.CreateAsync();
        World.Inject(new List<ItemTargetMonsterDto>
        {
            new() { ItemTargetMonsterId = 1, ItemVNum = Funnel, NpcMonsterVNum = KenkoSwordsman },
        });
        _gameEvents = World.Freeze<Mock<IGameEventDispatcher>>();
        _handler = World.Create<SuctionFunnelHandler>();
        _session = await World.CreateSessionAsync();
        _map = World.GetBaseMap(1);
        _session.Character.MapInstance = _map;
        _monster = Spawn(KenkoSwordsman);
        _item = InventoryItemInstance.Create(new FunnelInstance { Amount = 2 }, _session.Character.CharacterId);
        _item.Type = NoscorePocketType.Main;
        _item.Slot = 0;
        _session.Character.InventoryService[_item.ItemInstanceId] = _item;
    }

    [TestMethod]
    public async Task AKenkoSoldierInTheRedIsDrainedForItsBead()
    {
        _monster.Hp = 1999;

        await UseOnAsync(_monster);

        Assert.IsFalse(_monster.IsAlive);
        Assert.AreEqual(0, _monster.Hp);
        Assert.AreEqual(1, _session.Character.InventoryService.CountItem(Bead));
        Assert.AreEqual((short)1, _item.ItemInstance.Amount);
        Assert.AreEqual(Game18NConstString.ReceivedThisItem,
            _session.LastPackets.OfType<SayiPacket>().Single().Message);
        var characterId = _session.Character.CharacterId;
        _gameEvents.Verify(e => e.RaiseAsync(It.Is<EntityDiedEvent>(d => IsKilledBy(d, characterId))), Times.Once);
    }

    [TestMethod]
    public async Task ALaterUseOnTheSameSoldierFindsItGone()
    {
        _monster.Hp = 100;

        await UseOnAsync(_monster);
        await UseOnAsync(_monster);

        Assert.AreEqual(1, _session.Character.InventoryService.CountItem(Bead));
        Assert.AreEqual((short)1, _item.ItemInstance.Amount);
        _gameEvents.Verify(e => e.RaiseAsync(It.IsAny<EntityDiedEvent>()), Times.Once);
    }

    [TestMethod]
    public async Task AKenkoSoldierAtAFifthOfItsHpIsRefused()
    {
        _monster.Hp = 2000;

        await UseOnAsync(_monster);

        Assert.IsTrue(_monster.IsAlive);
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
        Assert.AreEqual(0, _session.Character.InventoryService.CountItem(Bead));
        Assert.AreEqual(Game18NConstString.CanBeUsedIfHpLessTwentyPercent,
            _session.LastPackets.OfType<MsgiPacket>().Single().Message);
        _gameEvents.Verify(e => e.RaiseAsync(It.IsAny<EntityDiedEvent>()), Times.Never);
    }

    [TestMethod]
    public async Task AMonsterTheFunnelDoesNotListIsLeftAlone()
    {
        var kovolt = Spawn(Kovolt);
        kovolt.Hp = 1;

        await UseOnAsync(kovolt);

        Assert.IsTrue(kovolt.IsAlive);
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
        Assert.AreEqual(0, _session.Character.InventoryService.CountItem(Bead));
    }

    [TestMethod]
    public async Task WithNoMonsterTargetedNothingHappens()
    {
        _monster.Hp = 1;

        await _handler.Handle(new ItemUsedEvent(_session, _item, new UseItemPacket
        {
            Type = PocketType.Main, Slot = 0, VisualType = VisualType.Player, VisualId = _session.Character.CharacterId,
        }));

        Assert.IsTrue(_monster.IsAlive);
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
    }

    [TestMethod]
    public async Task AnotherEffectIsIgnored()
    {
        _item.ItemInstance.Item.Effect = ItemEffectType.Teleport;
        _monster.Hp = 1;

        await UseOnAsync(_monster);

        Assert.IsTrue(_monster.IsAlive);
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
    }

    private static bool IsKilledBy(EntityDiedEvent died, long characterId) =>
        died.Killer is { VisualType: VisualType.Player } killer && killer.VisualId == characterId;

    private MonsterComponentBundle Spawn(short vnum) =>
        _map.SpawnMonster(new NpcMonsterDto { NpcMonsterVNum = vnum, MaxHp = 10000, Level = 44, IsHostile = true }, 5, 5, false);

    private Task UseOnAsync(MonsterComponentBundle monster) => _handler.Handle(new ItemUsedEvent(_session, _item,
        new UseItemPacket { Type = PocketType.Main, Slot = 0, VisualType = VisualType.Monster, VisualId = monster.VisualId }));

    private sealed class FunnelInstance : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
    {
        public new Guid Id { get; set; } = Guid.NewGuid();
        public new short ItemVNum { get; set; } = Funnel;
        public Item Item { get; set; } = new()
        {
            VNum = Funnel, Type = NoscorePocketType.Main, Effect = ItemEffectType.SuctionFunnel, EffectValue = Bead
        };
        public object Clone() => MemberwiseClone();
    }
}
