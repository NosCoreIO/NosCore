//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
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
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class ProductionCouponHandlerTests
    {
        private TestWorld World;
        private const short CouponVNum = 1271;

        private ClientSession _session;
        private ProductionCouponHandler _handler;
        private InventoryItemInstance _coupon;
        private Miniland _miniland;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _miniland = new Miniland { MinilandPoint = 100 };
            var characterId = _session.Character.CharacterId;
            World.Freeze<Mock<IMinilandService>>()
                .Setup(s => s.GetMiniland(characterId)).Returns(_miniland);
            _handler = World.Create<ProductionCouponHandler>();
        }

        [TestMethod]
        public async Task ACouponAddsItsPointsToTheOwnMinilandAndIsUsedUp()
        {
            Give(ItemEffectType.ProductionCoupon, points: 500);

            await UseAsync();

            Assert.AreEqual(600, _miniland.MinilandPoint);
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(CouponVNum));
            var msgi = _session.LastPackets.OfType<MsgiPacket>().Single();
            Assert.AreEqual(Game18NConstString.ProductionPointsToppedUp, msgi.Message);
            Assert.AreEqual(500L, Convert.ToInt64(msgi.Game18NArguments[0]));
        }

        [TestMethod]
        public async Task AnotherEffectIsLeftToItsOwnHandler()
        {
            Give(ItemEffectType.MinigameDurabilityCoupon, points: 300);

            await UseAsync();

            Assert.AreEqual(100, _miniland.MinilandPoint);
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(CouponVNum));
        }

        private void Give(ItemEffectType effect, int points)
        {
            var item = new Item
            {
                VNum = CouponVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Main,
                Effect = effect,
                EffectValue = points,
            };
            var instance = new ItemInstanceForTest(CouponVNum) { Amount = 1, Item = item };
            _coupon = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _coupon.Slot = 2;
            _coupon.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_coupon.ItemInstanceId] = _coupon;
        }

        private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _coupon, new UseItemPacket
        {
            VisualType = NosCore.Shared.Enumerations.VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Main,
            Slot = 2,
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
