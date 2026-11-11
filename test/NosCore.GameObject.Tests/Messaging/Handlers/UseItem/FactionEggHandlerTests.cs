using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Interfaces;
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
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem;

[TestClass]
public class FactionEggHandlerTests
{
    private TestWorld World;
    private ClientSession _session;
    private InventoryItemInstance _item;
    private readonly FactionEggHandler _handler = new();

    [TestInitialize]
    public async Task SetupAsync()
    {
        World = await TestWorld.CreateAsync();
        _session = await World.GenerateSessionAsync();
        _session.Character.MapInstance = World.GetBaseMap(1);
        _session.Character.Hp = 100;
        _session.Character.CharacterDto.Faction = FactionType.Neutral;
        _session.Character.CharacterDto.Act4Points = 123;
        var instance = new EggInstance { Amount = 2 };
        _item = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
        _item.Type = NoscorePocketType.Main;
        _item.Slot = 0;
        _session.Character.InventoryService[_item.ItemInstanceId] = _item;
    }

    /// <summary>
    /// Verifies that each faction prompts for confirmation without consuming the egg or changing character state.
    /// </summary>
    [TestMethod]
    [DataRow(FactionType.Angel)]
    [DataRow(FactionType.Demon)]
    public async Task FirstUseAsksWithoutChangingAnything(FactionType faction)
    {
        _item.ItemInstance.Item.EffectValue = (int)faction;
        await UseAsync(0);
        AssertUnchanged();
        var question = _session.LastPackets.OfType<QnaiPacket>().Single();
        Assert.AreEqual(faction == FactionType.Angel ? Game18NConstString.MessageToChangeFactionToAngelWithPointsReset
            : Game18NConstString.MessageToChangeFactionToDemonWithPointsReset, question.Question);
        Assert.AreEqual((byte)1, ((UseItemPacket)question.YesPacket.NotNull()).Mode);
    }

    [TestMethod]
    [DataRow(FactionType.Neutral, FactionType.Angel)]
    [DataRow(FactionType.Neutral, FactionType.Demon)]
    [DataRow(FactionType.Angel, FactionType.Demon)]
    [DataRow(FactionType.Demon, FactionType.Angel)]
    public async Task ConfirmationChangesFactionAndConsumesOneEgg(FactionType previous, FactionType target)
    {
        _session.Character.CharacterDto.Faction = previous;
        _item.ItemInstance.Item.EffectValue = (int)target;
        await UseAsync(1);
        Assert.AreEqual(target, _session.Character.CharacterDto.Faction);
        Assert.AreEqual(0, _session.Character.CharacterDto.Act4Points);
        Assert.AreEqual((short)1, _item.ItemInstance.Amount);
        Assert.AreEqual(target, _session.LastPackets.OfType<FsPacket>().Single().Faction);
        Assert.AreEqual(target, _session.Character.GenerateIn(string.Empty).InCharacterSubPacket.NotNull().Faction);
    }

    /// <summary>
    /// Verifies that an egg matching the current faction preserves items and points and reports the matching refusal.
    /// </summary>
    [TestMethod]
    [DataRow(FactionType.Angel)]
    [DataRow(FactionType.Demon)]
    public async Task SameFactionKeepsEggAndPoints(FactionType faction)
    {
        _session.Character.CharacterDto.Faction = faction;
        _item.ItemInstance.Item.EffectValue = (int)faction;
        await UseAsync(1);
        AssertUnchanged(faction);
        Assert.AreEqual(faction == FactionType.Angel ? Game18NConstString.AlreadyAngelFaction : Game18NConstString.AlreadyDemonFaction,
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
    [DataRow(-1)]
    [DataRow(257)]
    [DataRow(258)]
    [DataRow(int.MaxValue)]
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

    [TestMethod]
    public async Task JoiningFamilyAfterPromptPreventsChange()
    {
        await UseAsync(0);
        Assert.IsTrue(_session.LastPackets.OfType<QnaiPacket>().Any());
        _session.Character.Family = new Family();
        await UseAsync(1);
        AssertUnchanged();
    }

    [TestMethod]
    public async Task FinalEggIsRemovedAndCannotBeReplayed()
    {
        _item.ItemInstance.Amount = 1;
        await UseAsync(1);
        Assert.AreEqual(FactionType.Angel, _session.Character.CharacterDto.Faction);
        Assert.AreEqual((short)0, _item.ItemInstance.Amount);
        Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType(_item.Slot, _item.Type));
        await UseAsync(1);
        Assert.AreEqual(1, _session.LastPackets.OfType<FsPacket>().Count());
    }

    [TestMethod]
    public async Task SuccessfulChangeBroadcastsFactionToMap()
    {
        var observer = await World.GenerateSessionAsync();
        observer.Character.MapInstance = _session.Character.MapInstance;
        var sessions = new Mock<ISessionGroup>();
        sessions.Setup(group => group.Broadcast(It.IsAny<IPacket[]>()))
            .Returns<IPacket[]>(async packets =>
            {
                foreach (var packet in packets)
                {
                    await observer.SendPacketAsync(packet);
                }
            });
        _session.Character.MapInstance.Sessions = sessions.Object;
        await UseAsync(1);
        var appearance = observer.LastPackets.OfType<InPacket>().Single();
        Assert.AreEqual(_session.Character.VisualId, appearance.VisualId);
        Assert.AreEqual(FactionType.Angel, appearance.InCharacterSubPacket.NotNull().Faction);
    }

    [TestMethod]
    public async Task ChangedFactionAndPointsSurvivePersistence()
    {
        await UseAsync(1);
        await World.CharacterDao.TryInsertOrUpdateAsync(_session.Character.CharacterDto);
        var characterId = _session.Character.CharacterId;
        var saved = await World.CharacterDao.FirstOrDefaultAsync(character =>
            character.CharacterId == characterId);
        Assert.IsNotNull(saved);
        Assert.AreEqual(FactionType.Angel, saved.Faction);
        Assert.AreEqual(0, saved.Act4Points);
    }

    private Task UseAsync(byte mode) => _handler.Handle(new ItemUsedEvent(_session, _item,
        new UseItemPacket { Type = PocketType.Main, Slot = 0, Mode = mode }));

    private void AssertUnchanged(FactionType faction = FactionType.Neutral)
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
            Effect = ItemEffectType.ChangeFaction, EffectValue = (int)FactionType.Angel
        };
        public object Clone() => MemberwiseClone();
    }
}
