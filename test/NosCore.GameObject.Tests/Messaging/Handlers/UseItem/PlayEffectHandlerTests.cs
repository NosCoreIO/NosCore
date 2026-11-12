//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class PlayEffectHandlerTests
    {
        private TestWorld World;
        private const short FireworkVNum = 1983;

        private ClientSession _session;
        private PlayEffectHandler _handler;
        private InventoryItemInstance _firework;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _handler = World.Create<PlayEffectHandler>();
        }

        [TestMethod]
        public async Task AFireworkGoesOffWithItsSoundAndIsUsedUp()
        {
            Give(ItemEffectType.PlayEffect, effect: 875, sound: 1558);

            await UseAsync();

            var map = _session.Character.MapInstance.LastPackets;
            Assert.IsTrue(map.OfType<EffectPacket>().Any(e => e.Id == 875));
            Assert.IsTrue(map.OfType<GuriPacket>().Any(g => g.Type == GuriPacketType.AfterSumming && g.Value == 1558));
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(FireworkVNum));
        }

        [TestMethod]
        public async Task AToyWithoutASoundOnlyShowsItsEffect()
        {
            Give(ItemEffectType.PlayEffect, effect: 5108, sound: 0);

            await UseAsync();

            var map = _session.Character.MapInstance.LastPackets;
            Assert.IsTrue(map.OfType<EffectPacket>().Any(e => e.Id == 5108));
            Assert.IsFalse(map.OfType<GuriPacket>().Any());
        }

        [TestMethod]
        public async Task AnotherEffectIsLeftToItsOwnHandler()
        {
            Give(ItemEffectType.NoEffect, effect: 875, sound: 0);

            await UseAsync();

            Assert.IsFalse(_session.Character.MapInstance.LastPackets.OfType<EffectPacket>().Any(e => e.Id == 875));
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(FireworkVNum));
        }

        private void Give(ItemEffectType effectType, int effect, short sound)
        {
            var item = new Item
            {
                VNum = FireworkVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Event,
                Effect = effectType,
                EffectValue = effect,
                UseSound = sound,
            };
            var instance = new ItemInstanceForTest(FireworkVNum) { Amount = 1, Item = item };
            _firework = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _firework.Slot = 5;
            _firework.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_firework.ItemInstanceId] = _firework;
        }

        private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _firework, new UseItemPacket
        {
            VisualType = NosCore.Shared.Enumerations.VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Main,
            Slot = 5,
            Mode = 1,
            Parameter = 0,
        }));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
