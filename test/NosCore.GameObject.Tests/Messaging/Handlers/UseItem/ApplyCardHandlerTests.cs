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
using NodaTime;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class ApplyCardHandlerTests
    {
        private TestWorld World;
        private const short AntibioticsVNum = 2178;
        private const short ImmunityCardId = 105;
        private const short InfectionCardId = 104;
        private const short ZombieGroup = 83;

        private ClientSession _session;
        private BuffService _buffs;
        private ApplyCardHandler _handler;
        private InventoryItemInstance _antibiotics;
        private CardDto _infection;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var cards = World.Freeze<Mock<ICardCatalog>>();
            var immunity = new CardDto { CardId = ImmunityCardId, GroupId = ZombieGroup, Level = 5, Duration = 3000 };
            _infection = new CardDto { CardId = InfectionCardId, GroupId = ZombieGroup, Level = 4, Duration = 600 };
            cards.Setup(c => c.GetCard(ImmunityCardId)).Returns(immunity);
            cards.Setup(c => c.GetCard(InfectionCardId)).Returns(_infection);
            cards.Setup(c => c.GetCardBCards(It.IsAny<short>())).Returns(Array.Empty<BCardDto>());
            _buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(_buffs);
            _handler = World.Create<ApplyCardHandler>();
        }

        private Instant Now => World.Clock.GetCurrentInstant();

        [TestMethod]
        public async Task TheAntibioticsGiveFiveMinutesOfZombieImmunityAndAreUsedUp()
        {
            GiveAntibiotics(ItemEffectType.ApplyCard, 2);

            await UseAsync();

            var buff = Buff(ImmunityCardId);
            Assert.IsNotNull(buff);
            Assert.AreEqual(300, (int)(buff.ExpiresAt - Now).TotalSeconds);
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(AntibioticsVNum));
        }

        [TestMethod]
        public async Task TheAntibioticsCureAZombieInfection()
        {
            await _buffs.ApplyAsync(_session.Character, _infection, Array.Empty<BCardDto>(), null);
            GiveAntibiotics(ItemEffectType.ApplyCard, 1);

            await UseAsync();

            Assert.IsNull(Buff(InfectionCardId));
            Assert.IsNotNull(Buff(ImmunityCardId));
        }

        [TestMethod]
        public async Task ADeadCharacterKeepsTheAntibiotics()
        {
            GiveAntibiotics(ItemEffectType.ApplyCard, 1);
            _session.Character.IsAlive = false;

            await UseAsync();

            Assert.IsNull(Buff(ImmunityCardId));
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(AntibioticsVNum));
        }

        [TestMethod]
        public async Task AnotherEffectIsLeftToItsOwnHandler()
        {
            GiveAntibiotics(ItemEffectType.BuffPotions, 1);

            await UseAsync();

            Assert.IsNull(Buff(ImmunityCardId));
            Assert.AreEqual(1, _session.Character.InventoryService.CountItem(AntibioticsVNum));
        }

        private BuffInstance? Buff(short cardId) =>
            _buffs.GetActiveBuffs(_session.Character).FirstOrDefault(b => b.CardId == cardId);

        private void GiveAntibiotics(ItemEffectType effect, short amount)
        {
            var item = new Item
            {
                VNum = AntibioticsVNum,
                Type = NoscorePocketType.Etc,
                ItemType = ItemType.Magical,
                Effect = effect,
                EffectValue = ImmunityCardId,
            };
            var instance = new ItemInstanceForTest(AntibioticsVNum) { Amount = amount, Item = item };
            _antibiotics = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _antibiotics.Slot = 0;
            _antibiotics.Type = NoscorePocketType.Etc;
            _session.Character.InventoryService[_antibiotics.ItemInstanceId] = _antibiotics;
        }

        private Task UseAsync() => _handler.Handle(new ItemUsedEvent(_session, _antibiotics, new UseItemPacket
        {
            VisualType = NosCore.Shared.Enumerations.VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Etc,
            Slot = 0,
            Mode = 1,
            Parameter = 0,
        }));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Etc };
            public object Clone() => MemberwiseClone();
        }
    }
}
