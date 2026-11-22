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
using NosCore.GameObject.Services.TransformationService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem;

[TestClass]
public class CostumeScrollHandlerTests
{
    private const short HalloweenScroll = 1913;
    private const short YakWarriorLook = 1446;
    private const short DustKingLook = 1276;

    private TestWorld World;
    private ClientSession _session;
    private InventoryItemInstance _scroll;
    private Mock<ITransformationService> _transformations;
    private CostumeScrollHandler _handler;

    [TestInitialize]
    public async Task SetupAsync()
    {
        World = await TestWorld.CreateAsync();
        World.Inject(new List<CostumeMorphDto>
        {
            new() { CostumeMorphId = 1, ItemVNum = HalloweenScroll, Morph = YakWarriorLook },
            new() { CostumeMorphId = 2, ItemVNum = HalloweenScroll, Morph = DustKingLook },
        });
        _transformations = World.Freeze<Mock<ITransformationService>>();
        _transformations.Setup(t => t.ChangeMonsterFormAsync(It.IsAny<ClientSession>(), It.IsAny<short>()))
            .Callback<ClientSession, short>((s, _) => s.Character.InMonsterForm = true)
            .Returns(Task.CompletedTask);
        _handler = World.Create<CostumeScrollHandler>();
        _session = await World.CreateSessionAsync();
        _scroll = InventoryItemInstance.Create(new ScrollInstance { Amount = 2 }, _session.Character.CharacterId);
        _scroll.Type = NoscorePocketType.Main;
        _scroll.Slot = 0;
        _session.Character.InventoryService[_scroll.ItemInstanceId] = _scroll;
    }

    [TestMethod]
    public async Task UsingAScrollUsesOneUpAndTransformsIntoOneOfItsLooks()
    {
        World.Random.Setup(r => r.Next(0, 2)).Returns(1);

        await UseAsync();

        _transformations.Verify(t => t.ChangeMonsterFormAsync(_session, DustKingLook), Times.Once);
        Assert.AreEqual((short)1, _scroll.ItemInstance.Amount);
        Assert.IsTrue(_session.LastPackets.OfType<IvnPacket>().Any());
    }

    [TestMethod]
    public async Task UsingItAgainTransformsBackWithoutUsingAnotherScroll()
    {
        await UseAsync();

        await UseAsync();

        _transformations.Verify(t => t.RemoveMonsterFormAsync(_session), Times.Once);
        _transformations.Verify(t => t.ChangeMonsterFormAsync(_session, It.IsAny<short>()), Times.Once);
        Assert.AreEqual((short)1, _scroll.ItemInstance.Amount);
    }

    [TestMethod]
    public async Task ASpecialistIsRefusedAndKeepsTheScroll()
    {
        _session.Character.UseSp = true;

        await UseAsync();

        TheScrollIsRefusedWith_(Game18NConstString.CantTrasformInMonsterWhileSpecialist);
    }

    [TestMethod]
    public async Task ARiderIsRefusedAndKeepsTheScroll()
    {
        _session.Character.IsVehicled = true;

        await UseAsync();

        TheScrollIsRefusedWith_(Game18NConstString.CannotTransformInMonsterInVehicle);
    }

    [TestMethod]
    public async Task AScrollWithNoLookListedIsKept()
    {
        _scroll.ItemInstance.Item.VNum = 1947;

        await UseAsync();

        _transformations.Verify(t => t.ChangeMonsterFormAsync(It.IsAny<ClientSession>(), It.IsAny<short>()), Times.Never);
        Assert.AreEqual((short)2, _scroll.ItemInstance.Amount);
    }

    [TestMethod]
    public async Task TheSameEffectOnAnotherKindOfItemIsIgnored()
    {
        _scroll.ItemInstance.Item.ItemType = ItemType.Map;

        await UseAsync();

        _transformations.Verify(t => t.ChangeMonsterFormAsync(It.IsAny<ClientSession>(), It.IsAny<short>()), Times.Never);
        Assert.AreEqual((short)2, _scroll.ItemInstance.Amount);
    }

    private void TheScrollIsRefusedWith_(Game18NConstString message)
    {
        PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = message },
            _session.LastPackets.OfType<MsgiPacket>().Single());
        _transformations.Verify(t => t.ChangeMonsterFormAsync(It.IsAny<ClientSession>(), It.IsAny<short>()), Times.Never);
        Assert.AreEqual((short)2, _scroll.ItemInstance.Amount);
    }

    private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _scroll,
        new UseItemPacket { Type = PocketType.Main, Slot = 0 }));

    private sealed class ScrollInstance : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
    {
        public new Guid Id { get; set; } = Guid.NewGuid();
        public new short ItemVNum { get; set; } = HalloweenScroll;
        public Item Item { get; set; } = new()
        {
            VNum = HalloweenScroll, Type = NoscorePocketType.Main, ItemType = ItemType.Special,
            Effect = ItemEffectType.CostumeScroll, EffectValue = 1
        };
        public object Clone() => MemberwiseClone();
    }
}
