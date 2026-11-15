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
        private TestWorld World;
        private const short BoxVNum = 1013;
        private const short RewardVNum = 1012;
        private const short SwordsmanReward = 1012;
        private const short MageReward = 1015;
        private ClientSession _session;
        private Mock<IDao<RollGeneratedItemDto, short>> _rollDao;
        private Mock<IItemCatalog> _catalog;
        private BoxEffectHandler _handler;
        private InventoryItemInstance _item;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _rollDao = World.Freeze<Mock<IDao<RollGeneratedItemDto, short>>>();
            _catalog = World.Freeze<Mock<IItemCatalog>>();
            _catalog.Setup(c => c.GetItem(SwordsmanReward)).Returns(new Item { VNum = SwordsmanReward, Class = 1 << (byte)CharacterClassType.Swordsman });
            _catalog.Setup(c => c.GetItem(MageReward)).Returns(new Item { VNum = MageReward, Class = 1 << (byte)CharacterClassType.Mage });
            _handler = World.Create<BoxEffectHandler>();
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

        [TestMethod]
        public async Task ASetIsGivenWhole()
        {
            await new Spec("Every line of the rolled set is given, each with its own Rdi")
                .Given(BoxInInventoryWithEffect_, ItemEffectType.BoxEffect)
                .And(RollPoolIs_, new List<RollGeneratedItemDto>
                {
                    Row(SwordsmanReward, 0),
                    Row(MageReward, 0),
                })
                .WhenAsync(UsingTheItem)
                .Then(BoxStackCountShouldBe_, (short)0)
                .And(InventoryShouldContain_, SwordsmanReward)
                .And(InventoryShouldContain_, MageReward)
                .And(RdiPacketsShouldBeSent_, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AClassBoxGivesTheSetTheOpenerCanWear()
        {
            await new Spec("A class box only rolls among the sets fitting the opener's class")
                .Given(BoxInInventoryWithEffect_, ItemEffectType.ClassBox)
                .And(CharacterClassIs_, CharacterClassType.Mage)
                .And(RollPoolIs_, new List<RollGeneratedItemDto>
                {
                    Row(SwordsmanReward, 0),
                    Row(MageReward, 1),
                })
                .WhenAsync(UsingTheItem)
                .Then(InventoryShouldContain_, MageReward)
                .And(InventoryShouldNotContain_, SwordsmanReward)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AClassBoxNoSetFitsStillGivesOne()
        {
            await new Spec("An opener no set fits still gets one of them")
                .Given(BoxInInventoryWithEffect_, ItemEffectType.ClassBox)
                .And(CharacterClassIs_, CharacterClassType.Adventurer)
                .And(RollPoolIs_, new List<RollGeneratedItemDto>
                {
                    Row(SwordsmanReward, 0),
                    Row(MageReward, 1),
                })
                .WhenAsync(UsingTheItem)
                .Then(BoxStackCountShouldBe_, (short)0)
                .And(RdiPacketsShouldBeSent_, 1)
                .ExecuteAsync();
        }

        private static RollGeneratedItemDto Row(short vnum, short group) => new()
        {
            OriginalItemVNum = BoxVNum,
            ItemGeneratedVNum = vnum,
            ItemGeneratedAmount = 1,
            Probability = 1,
            RollGroup = group,
        };

        private void CharacterClassIs_(CharacterClassType characterClass) => _session.Character.Class = characterClass;

        private void InventoryShouldNotContain_(short vnum) =>
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(vnum));

        private void RdiPacketsShouldBeSent_(int count) =>
            Assert.AreEqual(count, _session.LastPackets.OfType<RdiPacket>().Count());

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
                .FirstOrDefault(p => p.Message == Game18NConstString.ReceivedThisItem);
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
