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
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class AncelloansBlessingHandlerTests
    {
        private TestWorld World;
        private const short BlessingVNum = 1286;

        private static readonly IReadOnlyList<BCardDto> BlessingBCards = new[]
        {
            new BCardDto { CardId = AncelloansBlessingHandler.BlessingCardId, Type = 44, SubType = 11, FirstData = 100 }
        };

        private ClientSession _session;
        private BuffService _buffs;
        private AncelloansBlessingHandler _handler;
        private InventoryItemInstance _blessing;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(c => c.GetCard(AncelloansBlessingHandler.BlessingCardId))
                .Returns(new CardDto { CardId = AncelloansBlessingHandler.BlessingCardId, Duration = 6001 });
            cards.Setup(c => c.GetCardBCards(AncelloansBlessingHandler.BlessingCardId)).Returns(BlessingBCards);
            _buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(_buffs);
            _handler = World.Create<AncelloansBlessingHandler>();
        }

        private Instant Now => World.Clock.GetCurrentInstant();

        [TestMethod]
        public async Task TheBlessingLastsAnHourAndUsesTheItemUp()
        {
            GiveBlessing(ItemEffectType.AncelloansBlessing, 1);

            await UseAsync();

            var buff = BlessingBuff();
            Assert.IsNotNull(buff);
            Assert.AreEqual(3600, (int)(buff.ExpiresAt - Now).TotalSeconds);
            Assert.IsTrue(buff.Static);
            Assert.AreEqual(0, _session.Character.InventoryService.CountItem(BlessingVNum));
        }

        [TestMethod]
        public async Task ABlessedCharacterKeepsTheSecondItem()
        {
            GiveBlessing(ItemEffectType.AncelloansBlessing, 2);
            await UseAsync();
            var expiresAt = BlessingBuff().NotNull().ExpiresAt;
            World.Clock.Advance(Duration.FromMinutes(10));

            await UseAsync();

            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(BlessingVNum));
            Assert.AreEqual(expiresAt, BlessingBuff().NotNull().ExpiresAt);
        }

        [TestMethod]
        public async Task TheBlessingSurvivesDeathAndRunsOutAfterAnHour()
        {
            GiveBlessing(ItemEffectType.AncelloansBlessing, 1);
            await UseAsync();

            await _buffs.ClearAsync(_session.Character);
            Assert.IsNotNull(BlessingBuff());

            World.Clock.Advance(Duration.FromHours(1) + Duration.FromSeconds(1));
            var expired = await _buffs.TickAsync(_session.Character);

            Assert.IsTrue(expired.Any(b => b.CardId == AncelloansBlessingHandler.BlessingCardId));
            Assert.IsFalse(_buffs.HasBuff(_session.Character, AncelloansBlessingHandler.BlessingCardId));
            var vb = _session.LastPackets.OfType<VbPacket>().Last();
            Assert.AreEqual(AncelloansBlessingHandler.BlessingCardId, vb.CardId);
            Assert.IsFalse(vb.IsActivated);
        }

        [TestMethod]
        public async Task AnotherEffectIsLeftToItsOwnHandler()
        {
            GiveBlessing(ItemEffectType.BuffPotions, 1);

            await UseAsync();

            Assert.IsNull(BlessingBuff());
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(BlessingVNum));
        }

        private NosCore.GameObject.Services.BattleService.Model.BuffInstance? BlessingBuff() =>
            System.Linq.Enumerable.FirstOrDefault(_buffs.GetActiveBuffs(_session.Character),
                b => b.CardId == AncelloansBlessingHandler.BlessingCardId);

        private void GiveBlessing(ItemEffectType effect, short amount)
        {
            var item = new Item
            {
                VNum = BlessingVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Special,
                Effect = effect,
            };
            var instance = new ItemInstanceForTest(BlessingVNum) { Amount = amount, Item = item };
            _blessing = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _blessing.Slot = 0;
            _blessing.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_blessing.ItemInstanceId] = _blessing;
        }

        private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _blessing, new UseItemPacket
        {
            VisualType = NosCore.Shared.Enumerations.VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Main,
            Slot = 0,
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
