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
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem;

[TestClass]
public class MinilandSignpostHandlerTests
{
    private const short LargeSignpost = 1085;
    private const short SignpostNpc = 921;

    private TestWorld World;
    private ClientSession _session;
    private MapInstance _nosVille;
    private InventoryItemInstance _item;
    private MinilandSignpostHandler _handler;

    [TestInitialize]
    public async Task SetupAsync()
    {
        World = await TestWorld.CreateAsync();
        World.Inject(new List<NpcMonsterDto> { new() { NpcMonsterVNum = SignpostNpc, MaxHp = 18000 } });
        World.UseReal<IMinilandSignpostService, MinilandSignpostService>();
        _handler = World.Create<MinilandSignpostHandler>();
        _session = await World.CreateSessionAsync();
        _nosVille = World.GetBaseMap(MinilandSignpostService.NosVilleMapId);
        _session.Character.MapInstance = _nosVille;
        _session.Character.PositionX = 5;
        _session.Character.PositionY = 7;
        _item = InventoryItemInstance.Create(new SignpostInstance { Amount = 2 }, _session.Character.CharacterId);
        _item.Type = NoscorePocketType.Main;
        _item.Slot = 0;
        _session.Character.InventoryService[_item.ItemInstanceId] = _item;
    }

    [TestMethod]
    public async Task UsingASignpostPutsItsNpcUpWhereThePlayerStandsForAnHour()
    {
        await UseAsync();

        var signpost = _nosVille.EnumerateNpcs().Single();
        Assert.AreEqual(SignpostNpc, signpost.NpcMonster?.NpcMonsterVNum);
        Assert.AreEqual((short)5, signpost.PositionX);
        Assert.AreEqual((short)7, signpost.PositionY);
        Assert.AreEqual(_session.Character.CharacterId, signpost.SignpostOwnerId);
        Assert.AreEqual(World.Clock.GetCurrentInstant() + SignpostSystem.Lifetime, signpost.SignpostExpiresAt);
        Assert.AreEqual((short)1, _item.ItemInstance.Amount);
        var shown = _nosVille.LastPackets.OfType<InPacket>().Single(p => p.VisualId == signpost.VisualId).InNonPlayerSubPacket;
        Assert.AreEqual(Game18NConstString.UserMiniland, shown?.Title);
        Assert.AreEqual(_session.Character.Name, shown?.TitleArguments[0]);
        Assert.AreEqual(100, shown?.InAliveSubPacket?.Hp);
    }

    [TestMethod]
    public async Task ASecondSignpostIsRefusedWithTheTimeTheFirstHasLeft()
    {
        await UseAsync();
        World.Clock.AdvanceMinutes(10);

        await UseAsync();

        Assert.AreEqual(1, _nosVille.EnumerateNpcs().Count());
        Assert.AreEqual((short)1, _item.ItemInstance.Amount);
        PacketAssert.AreEqual(new MsgiPacket
        {
            Type = MessageType.Default,
            Message = Game18NConstString.AlreadyASignPost,
            ArgumentType = 4,
            Game18NArguments = { 3000L },
        }, _session.LastPackets.OfType<MsgiPacket>().Single());
    }

    [TestMethod]
    public async Task OnceTheFirstSignpostsTimeIsUpAnotherCanGoUp()
    {
        await UseAsync();
        World.Clock.Advance(SignpostSystem.Lifetime);

        await UseAsync();

        Assert.IsFalse(_session.LastPackets.OfType<MsgiPacket>().Any());
        Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType(_item.Slot, _item.Type));
    }

    [TestMethod]
    public async Task ASignpostWhoseTimeIsUpDoesNotHideTheOneStillUp()
    {
        _item.ItemInstance.Amount = 3;
        await UseAsync();
        World.Clock.Advance(SignpostSystem.Lifetime);
        await UseAsync();
        World.Clock.AdvanceMinutes(10);

        await UseAsync();

        Assert.AreEqual((short)1, _item.ItemInstance.Amount);
        Assert.AreEqual(Game18NConstString.AlreadyASignPost, _session.LastPackets.OfType<MsgiPacket>().Single().Message);
    }

    [TestMethod]
    public async Task ASignpostOutsideNosVilleIsRefusedAndKept()
    {
        _session.Character.MapInstance = World.GetBaseMap(TestWorld.TestMapId);

        await UseAsync();

        Assert.IsFalse(World.GetBaseMap(TestWorld.TestMapId).EnumerateNpcs().Any(n => n.SignpostOwnerId != 0));
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
        PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.SignPostOnlyNosville },
            _session.LastPackets.OfType<MsgiPacket>().Single());
    }

    [TestMethod]
    public async Task AnotherEffectIsIgnored()
    {
        _item.ItemInstance.Item.Effect = ItemEffectType.Teleport;

        await UseAsync();

        Assert.IsFalse(_nosVille.EnumerateNpcs().Any());
        Assert.AreEqual((short)2, _item.ItemInstance.Amount);
    }

    private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _item,
        new UseItemPacket { Type = PocketType.Main, Slot = 0 }));

    private sealed class SignpostInstance : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
    {
        public new Guid Id { get; set; } = Guid.NewGuid();
        public new short ItemVNum { get; set; } = LargeSignpost;
        public Item Item { get; set; } = new()
        {
            VNum = LargeSignpost, Type = NoscorePocketType.Main, ItemType = ItemType.Special,
            Effect = ItemEffectType.MinilandSignpost, EffectValue = SignpostNpc
        };
        public object Clone() => MemberwiseClone();
    }
}
