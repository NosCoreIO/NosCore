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
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class NpcHealingItemHandlerTests
    {
        private const short LuiniaVNum = 2127;
        private const int Heal = 800;
        private const int NpcMaxHp = 3000;
        private const short UserX = 20;
        private const short UserY = 20;

        private TestWorld World;
        private ClientSession _session;
        private NpcHealingItemHandler _handler;
        private InventoryItemInstance _luinia;
        private NpcComponentBundle _near;
        private NpcComponentBundle _otherNear;
        private NpcComponentBundle _far;
        private MonsterComponentBundle _monster;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.PositionX = UserX;
            _session.Character.PositionY = UserY;
            _handler = World.Create<NpcHealingItemHandler>();
        }

        [TestMethod]
        public async Task HurtNpcsInReachGetTheItemsHpBack()
        {
            await new Spec("Every hurt NPC within reach gets the item's HP back, never past its maximum, and one item is used up")
                .Given(TheUserHoldsAnItemReaching_Cells, (byte)6)
                .And(AnNpc_CellsAwayHas_Hp, 6, 1000)
                .And(AnotherNpcNextToTheUserHas_Hp, NpcMaxHp - 100)
                .WhenAsync(UsingTheItem)
                .Then(TheNpcHas_Hp, 1000 + Heal)
                .And(TheOtherNpcHas_Hp, NpcMaxHp)
                .And(TheMapSees_HealNumbers, 2)
                .And(TheUserHolds_Items, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnNpcOutOfReachIsLeftAlone()
        {
            await new Spec("An NPC one cell beyond the reach is not healed and nothing is used up")
                .Given(TheUserHoldsAnItemReaching_Cells, (byte)6)
                .And(AFarNpc_CellsAwayHas_Hp, 7, 1000)
                .WhenAsync(UsingTheItem)
                .Then(TheFarNpcHas_Hp, 1000)
                .And(TheUserHolds_Items, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMonsterInReachIsNotHealed()
        {
            await new Spec("A hurt monster next to the user is not an NPC and gets nothing")
                .Given(TheUserHoldsAnItemReaching_Cells, (byte)6)
                .And(AMonsterNextToTheUserHas_Hp, 1000)
                .WhenAsync(UsingTheItem)
                .Then(TheMonsterHas_Hp, 1000)
                .And(TheUserHolds_Items, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NothingIsUsedUpWhenNoNpcIsHurt()
        {
            await new Spec("With every NPC in reach at full HP the item stays in the bag")
                .Given(TheUserHoldsAnItemReaching_Cells, (byte)6)
                .And(AnotherNpcNextToTheUserHas_Hp, NpcMaxHp)
                .WhenAsync(UsingTheItem)
                .Then(TheMapSees_HealNumbers, 0)
                .And(TheUserHolds_Items, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnItemWithoutReachIsLeftToItsOwnHandler()
        {
            await new Spec("An item that heals no NPC leaves a hurt NPC next to the user alone")
                .Given(TheUserHoldsAnItemReaching_Cells, (byte)0)
                .And(AnotherNpcNextToTheUserHas_Hp, 1000)
                .WhenAsync(UsingTheItem)
                .Then(TheOtherNpcHas_Hp, 1000)
                .And(TheUserHolds_Items, 1)
                .ExecuteAsync();
        }

        private void TheUserHoldsAnItemReaching_Cells(byte reach)
        {
            var item = new Item
            {
                VNum = LuiniaVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Magical,
                EffectValue = Heal,
                HealsNpcsWithin = reach,
            };
            var instance = new ItemInstanceForTest(LuiniaVNum) { Amount = 1, Item = item };
            _luinia = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _luinia.Slot = 5;
            _luinia.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_luinia.ItemInstanceId] = _luinia;
        }

        private NpcComponentBundle PlaceNpc(short x, short y, int hp)
        {
            var npc = _session.Character.MapInstance.PlaceNpc(
                new NpcMonsterDto { NpcMonsterVNum = 320, MaxHp = NpcMaxHp }, x, y);
            npc.Hp = hp;
            return npc;
        }

        private void AnNpc_CellsAwayHas_Hp(int cells, int hp) => _near = PlaceNpc((short)(UserX + cells), (short)(UserY - cells), hp);

        private void AnotherNpcNextToTheUserHas_Hp(int hp) => _otherNear = PlaceNpc(UserX + 1, UserY, hp);

        private void AFarNpc_CellsAwayHas_Hp(int cells, int hp) => _far = PlaceNpc(UserX, (short)(UserY + cells), hp);

        private void AMonsterNextToTheUserHas_Hp(int hp)
        {
            _monster = _session.Character.MapInstance.SummonMonster(
                new NpcMonsterDto { NpcMonsterVNum = 67, MaxHp = NpcMaxHp }, UserX, UserY + 1, true);
            _monster.Hp = hp;
        }

        private Task UsingTheItem() => _handler.Handle(new ItemUsedEvent(_session, _luinia, new UseItemPacket
        {
            VisualType = VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Main,
            Slot = 5,
            Mode = 1,
            Parameter = 0,
        }));

        private void TheNpcHas_Hp(int hp) => Assert.AreEqual(hp, _near.Hp);

        private void TheOtherNpcHas_Hp(int hp) => Assert.AreEqual(hp, _otherNear.Hp);

        private void TheFarNpcHas_Hp(int hp) => Assert.AreEqual(hp, _far.Hp);

        private void TheMonsterHas_Hp(int hp) => Assert.AreEqual(hp, _monster.Hp);

        private void TheMapSees_HealNumbers(int count) =>
            Assert.AreEqual(count, _session.Character.MapInstance.LastPackets.OfType<RcPacket>()
                .Count(p => p.VisualType == VisualType.Npc));

        private void TheUserHolds_Items(int count) =>
            Assert.AreEqual(count, _session.Character.InventoryService.CountItem(LuiniaVNum));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
