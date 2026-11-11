//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.SpecialistPointService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemInfoService;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class SpPointInitializerHandlerTests
    {
        private const short PotionVNum = 1366;
        private ClientSession _session;
        private SpecialistInstance _card;
        private SpPointInitializerHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            var itemInfo = new Mock<IItemInfoService>();
            itemInfo.Setup(i => i.GenerateInfo(It.IsAny<IItemInstance>())).Returns(new SlInfoPacket());
            _handler = new SpPointInitializerHandler(new SpecialistCardService(new SpecialistPointService()),
                itemInfo.Object, new Mock<IBattleStatsProvider>().Object, new Mock<IVitalityService>().Object);
        }

        [TestMethod]
        public async Task ThePotionGivesBackEverySpentPoint()
        {
            await new Spec("The reset gives back every point spent on the worn card and uses one potion")
                .Given(CharacterWearsACardWithPointsSpent)
                .And(CharacterIsTransformed)
                .And(CharacterHas_Potions, 2)
                .WhenAsync(AskingForTheReset)
                .Then(TheCardShouldHaveNoPointsSpent)
                .And(PotionCountShouldBe_, 1)
                .And(TheMessageShouldBe_, Game18NConstString.PointsRestored)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutAPotionNothingIsReset()
        {
            await new Spec("Without a potion the points stay and the player is told")
                .Given(CharacterWearsACardWithPointsSpent)
                .And(CharacterIsTransformed)
                .WhenAsync(AskingForTheReset)
                .Then(TheCardShouldKeepItsPoints)
                .And(TheMessageShouldBe_, Game18NConstString.NotEnoughSpecialistPoints)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACardWithNothingSpentKeepsThePotion()
        {
            await new Spec("A card with nothing spent is already reset and the potion is kept")
                .Given(CharacterWearsACardWithNothingSpent)
                .And(CharacterIsTransformed)
                .And(CharacterHas_Potions, 1)
                .WhenAsync(AskingForTheReset)
                .Then(PotionCountShouldBe_, 1)
                .And(TheMessageShouldBe_, Game18NConstString.SpecialistCardAlreadyReset)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheResetNeedsTheTransformation()
        {
            await new Spec("Without the transformation the reset is refused and the potion kept")
                .Given(CharacterWearsACardWithPointsSpent)
                .And(CharacterHas_Potions, 1)
                .WhenAsync(AskingForTheReset)
                .Then(TheCardShouldKeepItsPoints)
                .And(PotionCountShouldBe_, 1)
                .And(TheMessageShouldBe_, Game18NConstString.SpecialistCardMustBeEquipped)
                .ExecuteAsync();
        }

        private void CharacterWearsACardWithPointsSpent()
        {
            CharacterWearsACardWithNothingSpent();
            _card.SlDamage = 20;
            _card.SlDefence = 10;
            _card.SlElement = 5;
            _card.SlHp = 3;
        }

        private void CharacterWearsACardWithNothingSpent()
        {
            _card = new SpecialistInstance(new Item { VNum = 912, ItemType = ItemType.Specialist, EquipmentSlot = EquipmentType.Sp })
            {
                SpLevel = 40,
            };
            var worn = InventoryItemInstance.Create(_card, _session.Character.CharacterId);
            worn.Type = NoscorePocketType.Wear;
            worn.Slot = (short)EquipmentType.Sp;
            _session.Character.InventoryService[worn.ItemInstanceId] = worn;
        }

        private void CharacterIsTransformed()
        {
            _session.Character.UseSp = true;
        }

        private void CharacterHas_Potions(int amount)
        {
            var potion = new ItemInstance(new Item
            {
                VNum = PotionVNum, Type = NoscorePocketType.Main, Effect = ItemEffectType.PointInitialisation
            })
            {
                Amount = (short)amount
            };
            var inv = InventoryItemInstance.Create(potion, _session.Character.CharacterId);
            inv.Type = NoscorePocketType.Main;
            inv.Slot = 0;
            _session.Character.InventoryService[inv.ItemInstanceId] = inv;
        }

        private Task AskingForTheReset() =>
            _handler.Handle(new GuriPacketReceivedEvent(_session, new ClientGuriPacket
            {
                Type = GuriPacketType.SpPointInitializer,
                Argument = 0,
            }));

        private void TheCardShouldHaveNoPointsSpent()
        {
            Assert.AreEqual(0, _card.SlDamage + _card.SlDefence + _card.SlElement + _card.SlHp);
        }

        private void TheCardShouldKeepItsPoints()
        {
            Assert.AreEqual(38, _card.SlDamage + _card.SlDefence + _card.SlElement + _card.SlHp);
        }

        private void PotionCountShouldBe_(int amount)
        {
            Assert.AreEqual(amount, _session.Character.InventoryService.Values
                .Where(i => i.ItemInstance.ItemVNum == PotionVNum).Sum(i => i.ItemInstance.Amount));
        }

        private void TheMessageShouldBe_(Game18NConstString message)
        {
            var packet = _session.LastPackets.OfType<MsgiPacket>().LastOrDefault();
            Assert.AreEqual(message, packet?.Message);
        }
    }
}
