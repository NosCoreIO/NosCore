//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.RaidService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class RaidSealHandlerTests
    {
        private const short CubySeal = 1127;

        private TestWorld World;
        private ClientSession _session;
        private InventoryItemInstance _seal;
        private Mock<IRaidService> _raids;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _raids = World.Freeze<Mock<IRaidService>>();
            _seal = InventoryItemInstance.Create(new SealInstance(), _session.Character.CharacterId);
            _seal.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_seal.ItemInstanceId] = _seal;
        }

        [TestMethod]
        public async Task UsingASealOpensItsRaid()
        {
            await UseAsync();

            _raids.Verify(s => s.OpenAsync(_session, _seal), Times.Once);
        }

        [TestMethod]
        public async Task AnotherEffectOpensNoRaid()
        {
            _seal.ItemInstance.Item.Effect = ItemEffectType.Teleport;

            await UseAsync();

            _raids.Verify(s => s.OpenAsync(It.IsAny<ClientSession>(), It.IsAny<InventoryItemInstance>()), Times.Never);
        }

        private Task UseAsync() => World.Create<RaidSealHandler>().Handle(new ItemUsedEvent(_session, _seal,
            new UseItemPacket { Type = PocketType.Main, Slot = 0 }));

        private sealed class SealInstance : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = CubySeal;
            public Item Item { get; set; } = new()
            {
                VNum = CubySeal, Type = NoscorePocketType.Main, Effect = ItemEffectType.RaidSeal
            };
            public object Clone() => MemberwiseClone();
        }
    }
}
