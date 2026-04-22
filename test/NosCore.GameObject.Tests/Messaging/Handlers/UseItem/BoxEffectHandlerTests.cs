//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class BoxEffectHandlerTests
    {
        private const short BoxVNum = 1013;
        private const short RewardVNum = 1012;
        private ClientSession _session = null!;
        private Mock<IDao<RollGeneratedItemDto, short>> _rollDao = null!;
        private BoxEffectHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _rollDao = new Mock<IDao<RollGeneratedItemDto, short>>();
            _handler = new BoxEffectHandler(TestHelpers.Instance.GenerateItemProvider(), _rollDao.Object);
        }

        [TestMethod]
        public async Task WrongEffectIsIgnored()
        {
            await new Spec("A non-BoxEffect item does not consult the roll table and is not consumed")
                .Given(BoxInInventoryWithEffect_, ItemEffectType.Teleport)
                .WhenAsync(UsingTheItem)
                .Then(BoxStackCountShouldBe_, (short)1)
                .And(RollTableShouldNotHaveBeenConsulted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EmptyRollPoolIsNoOp()
        {
            await new Spec("A BoxEffect with no matching roll rows returns without rewarding or consuming")
                .Given(BoxInInventoryWithEffect_, ItemEffectType.BoxEffect)
                .And(RollPoolIs_, new List<RollGeneratedItemDto>())
                .WhenAsync(UsingTheItem)
                .Then(BoxStackCountShouldBe_, (short)1)
                .And(NoRdiPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ZeroProbabilitySumIsNoOp()
        {
            await new Spec("A BoxEffect whose pool sums to zero probability cannot roll and is not consumed")
                .Given(BoxInInventoryWithEffect_, ItemEffectType.BoxEffect)
                .And(RollPoolIs_, new List<RollGeneratedItemDto>
                {
                    new()
                    {
                        OriginalItemVNum = BoxVNum,
                        ItemGeneratedVNum = RewardVNum,
                        ItemGeneratedAmount = 1,
                        Probability = 0,
                    },
                })
                .WhenAsync(UsingTheItem)
                .Then(BoxStackCountShouldBe_, (short)1)
                .And(NoRdiPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ValidRollConsumesBoxAndRewardsPlayer()
        {
            await new Spec("A single-entry pool always wins: reward lands in inventory, box is consumed, Rdi + ItemReceived go out")
                .Given(BoxInInventoryWithEffect_, ItemEffectType.BoxEffect)
                .And(RollPoolIs_, new List<RollGeneratedItemDto>
                {
                    new()
                    {
                        OriginalItemVNum = BoxVNum,
                        ItemGeneratedVNum = RewardVNum,
                        ItemGeneratedAmount = 1,
                        Probability = 100,
                    },
                })
                .WhenAsync(UsingTheItem)
                .Then(BoxStackCountShouldBe_, (short)0)
                .And(InventoryShouldContain_, RewardVNum)
                .And(RdiPacketShouldBeSentFor_, RewardVNum)
                .And(ItemReceivedShouldBeSent)
                .ExecuteAsync();
        }

        private void BoxInInventoryWithEffect_(ItemEffectType effect)
        {
            var item = new Item
            {
                VNum = BoxVNum,
                Type = NoscorePocketType.Main,
                Effect = effect,
            };
            var inst = new ItemInstanceForTest(BoxVNum) { Amount = 1, Item = item, Rare = 0, Design = 0 };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private void RollPoolIs_(List<RollGeneratedItemDto> pool) =>
            _rollDao.Setup(x => x.Where(It.IsAny<Expression<Func<RollGeneratedItemDto, bool>>>()))
                .Returns(pool);

        private async Task UsingTheItem()
        {
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 1,
                Parameter = 0,
            };
            await _handler.Handle(new ItemUsedEvent(_session, _item, packet));
        }

        private void BoxStackCountShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(BoxVNum));

        private void InventoryShouldContain_(short vnum) =>
            Assert.IsTrue(_session.Character.InventoryService.CountItem(vnum) > 0);

        private void RollTableShouldNotHaveBeenConsulted() =>
            _rollDao.Verify(x => x.Where(It.IsAny<Expression<Func<RollGeneratedItemDto, bool>>>()),
                Times.Never);

        private void NoRdiPacketShouldBeSent() =>
            Assert.IsFalse(_session.LastPackets.OfType<RdiPacket>().Any());

        private void RdiPacketShouldBeSentFor_(short vnum)
        {
            var rdi = _session.LastPackets.OfType<RdiPacket>().LastOrDefault();
            Assert.IsNotNull(rdi);
            Assert.AreEqual(vnum, rdi.ItemVNum);
        }

        private void ItemReceivedShouldBeSent()
        {
            var say = _session.LastPackets.OfType<SayiPacket>()
                .FirstOrDefault(p => p.Message == Game18NConstString.ItemReceived);
            Assert.IsNotNull(say);
        }

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
