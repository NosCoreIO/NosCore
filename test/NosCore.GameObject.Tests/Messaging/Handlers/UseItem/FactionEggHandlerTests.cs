using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem;

[TestClass]
public class FactionEggHandlerTests
{
    private ClientSession _session = null!;
    private InventoryItemInstance _item = null!;
    private readonly FactionEggHandler _handler = new();

    [TestInitialize]
    public async Task SetupAsync()
    {
        await TestHelpers.ResetAsync();
        _session = await TestHelpers.Instance.GenerateSessionAsync();
        _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        _session.Character.Hp = 100;
        _session.Character.CharacterDto.Faction = 0;
        _session.Character.CharacterDto.Act4Points = 123;
        var instance = new EggInstance { Amount = 2 };
        _item = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
        _item.Type = NoscorePocketType.Main;
        _item.Slot = 0;
        _session.Character.InventoryService[_item.ItemInstanceId] = _item;
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public async Task FirstUseAsksWithoutChangingAnything(int faction)
    {
        _item.ItemInstance.Item.EffectValue = faction;
        await UseAsync(0);
        AssertUnchanged();
        var question = _session.LastPackets.OfType<QnaiPacket>().Single();
        Assert.AreEqual(faction == 1 ? Game18NConstString.MessageToChangeFactionToAngelWithPointsReset
            : Game18NConstString.MessageToChangeFactionToDemonWithPointsReset, question.Question);
        Assert.AreEqual((byte)1, ((UseItemPacket)question.YesPacket!).Mode);
    }

    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(0, 2)]
    [DataRow(1, 2)]
    [DataRow(2, 1)]
    public async Task ConfirmationChangesFactionAndConsumesOneEgg(int previous, int target)
    {
        _session.Character.CharacterDto.Faction = (byte)previous;
        _item.ItemInstance.Item.EffectValue = target;
        await UseAsync(1);
        Assert.AreEqual((byte)target, _session.Character.CharacterDto.Faction);
        Assert.AreEqual(0, _session.Character.CharacterDto.Act4Points);
        Assert.AreEqual((short)1, _item.ItemInstance.Amount);
        Assert.AreEqual((FactionType)target, _session.LastPackets.OfType<FsPacket>().Single().Faction);
        Assert.AreEqual((FactionType)target, _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.Faction);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public async Task SameFactionKeepsEggAndPoints(int faction)
    {
        _session.Character.CharacterDto.Faction = (byte)faction;
        _item.ItemInstance.Item.EffectValue = faction;
        await UseAsync(1);
        AssertUnchanged((byte)faction);
        Assert.AreEqual(faction == 1 ? Game18NConstString.AlreadyAngelFaction : Game18NConstString.AlreadyDemonFaction,
            _session.LastPackets.OfType<MsgiPacket>().Single().Message);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public async Task FamilyMembershipIsRecheckedOnConfirmation(int mode)
    {
        _session.Character.Family = new Family();
        await UseAsync((byte)mode);
        AssertUnchanged();
        Assert.IsFalse(_session.LastPackets.OfType<QnaiPacket>().Any());
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public async Task UnsupportedEffectValueIsIgnored(int value)
    {
        _item.ItemInstance.Item.EffectValue = value;
        await UseAsync(1);
        AssertUnchanged();
    }

    [TestMethod]
    public async Task WrongEffectIsIgnored()
    {
        _item.ItemInstance.Item.Effect = ItemEffectType.Teleport;
        await UseAsync(1);
        AssertUnchanged();
    }

    [TestMethod]
    public async Task UnknownModeIsIgnored()
    {
        await UseAsync(2);
        AssertUnchanged();
    }

    [TestMethod]
    public async Task RemovedEggCannotBeReplayed()
    {
        _session.Character.InventoryService.Remove(_item.ItemInstanceId);
        await UseAsync(1);
        AssertUnchanged();
    }

    [TestMethod]
    public async Task DeadCharacterCannotUseEgg()
    {
        _session.Character.Hp = 0;
        await UseAsync(1);
        AssertUnchanged();
    }

    [TestMethod]
    public async Task TradingCharacterCannotUseEgg()
    {
        _session.Character.InExchange = true;
        await UseAsync(1);
        AssertUnchanged();
    }

    private Task UseAsync(byte mode) => _handler.Handle(new ItemUsedEvent(_session, _item,
        new UseItemPacket { Type = PocketType.Main, Slot = 0, Mode = mode }));

    private void AssertUnchanged(byte faction = 0)
    {
        Assert.AreEqual(faction, _session.Character.CharacterDto.Faction);
        Assert.AreEqual(123, _session.Character.CharacterDto.Act4Points);
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
        Assert.IsFalse(_session.LastPackets.OfType<FsPacket>().Any());
    }

    private sealed class EggInstance : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
    {
        public new Guid Id { get; set; } = Guid.NewGuid();
        public new short ItemVNum { get; set; } = 1624;
        public Item Item { get; set; } = new()
        {
            VNum = 1624, Type = NoscorePocketType.Main,
            Effect = ItemEffectType.ChangeFaction, EffectValue = 1
        };
        public object Clone() => MemberwiseClone();
    }
}
