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
using NodaTime;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class FairyBoosterHandlerTests
    {
        private TestWorld World;
        private const short BoosterVNum = 1296;

        private ClientSession _session;
        private BuffService _buffs;
        private FairyBoosterHandler _handler;
        private InventoryItemInstance _booster;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(c => c.GetCard(FairyBooster.CardId))
                .Returns(new CardDto { CardId = FairyBooster.CardId, Duration = 6001 });
            cards.Setup(c => c.GetCardBCards(FairyBooster.CardId)).Returns(new List<BCardDto>());
            _buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(_buffs);
            _handler = World.Create<FairyBoosterHandler>();
        }

        [TestMethod]
        public async Task TheBoosterLastsAnHourAndMakesTheWornFairyGlow()
        {
            var fairy = WearAFairy();
            GiveBoosters(ItemEffectType.FairyBooster, 1);

            await UseAsync();

            var buff = _buffs.GetActiveBuffs(_session.Character).Single(b => b.CardId == FairyBooster.CardId);
            Assert.AreEqual(3600, (int)(buff.ExpiresAt - World.Clock.GetCurrentInstant()).TotalSeconds);
            Assert.IsTrue(buff.Static);
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(BoosterVNum));
            Assert.AreEqual(fairy.Item.Morph + FairyBooster.MorphBonus, _session.Character.GeneratePairy(fairy).Morph);
        }

        [TestMethod]
        public async Task ABoostedCharacterKeepsTheSecondBooster()
        {
            GiveBoosters(ItemEffectType.FairyBooster, 2);
            await UseAsync();
            World.Clock.Advance(Duration.FromMinutes(10));

            await UseAsync();

            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(BoosterVNum));
        }

        [TestMethod]
        public async Task OnceItRunsOutTheFairyStopsGlowing()
        {
            var fairy = WearAFairy();
            GiveBoosters(ItemEffectType.FairyBooster, 1);
            await UseAsync();

            World.Clock.Advance(Duration.FromHours(1) + Duration.FromSeconds(1));
            await _buffs.TickAsync(_session.Character);

            Assert.AreEqual(fairy.Item.Morph, _session.Character.GeneratePairy(fairy).Morph);
        }

        [TestMethod]
        public async Task AnotherEffectIsLeftToItsOwnHandler()
        {
            GiveBoosters(ItemEffectType.AncelloansBlessing, 1);

            await UseAsync();

            Assert.IsFalse(_buffs.HasBuff(_session.Character, FairyBooster.CardId));
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(BoosterVNum));
        }

        private WearableInstance WearAFairy()
        {
            var fairy = _session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(World.GenerateItemProvider().Create(2, 1), _session.Character.CharacterId)).NotNull()
                .First();
            fairy.Type = NoscorePocketType.Wear;
            fairy.Slot = (byte)EquipmentType.Fairy;
            return (WearableInstance)fairy.ItemInstance;
        }

        private void GiveBoosters(ItemEffectType effect, short amount)
        {
            var item = new Item
            {
                VNum = BoosterVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Special,
                Effect = effect,
            };
            var instance = new ItemInstanceForTest(BoosterVNum) { Amount = amount, Item = item };
            _booster = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _booster.Slot = 5;
            _booster.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_booster.ItemInstanceId] = _booster;
        }

        private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _booster, new UseItemPacket
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
