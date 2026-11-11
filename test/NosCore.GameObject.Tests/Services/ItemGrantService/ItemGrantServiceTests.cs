//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemGrantService;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ItemGrantService
{
    [TestClass]
    public class ItemGrantServiceTests
    {
        private TestWorld World;
        private const short PotionVNum = 1012;
        private const short WeaponVNum = 1;
        private const short SpecialistVNum = 912;

        private GameObject.Services.ItemGrantService.ItemGrantService Service;
        private ClientSession Session;
        private Mock<IItemGenerationService> ItemProvider;
        private ItemGrantResult Result;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            ItemProvider = new Mock<IItemGenerationService>();
            var items = new List<ItemDto>
            {
                new Item { VNum = PotionVNum, Type = NoscorePocketType.Main },
                new Item { VNum = WeaponVNum, Type = NoscorePocketType.Equipment, EquipmentSlot = EquipmentType.MainWeapon, ItemType = ItemType.Weapon },
                new Item { VNum = SpecialistVNum, Type = NoscorePocketType.Equipment, EquipmentSlot = EquipmentType.Sp, ItemType = ItemType.Specialist }
            };

            ItemProvider.Setup(x => x.Create(It.IsAny<short>(), It.IsAny<short>(), It.IsAny<sbyte>(), It.IsAny<byte>(), It.IsAny<byte>()))
                .Returns((short vnum, short amount, sbyte rare, byte upgrade, byte design) =>
                    new ItemInstance(new Item { VNum = vnum, Type = NoscorePocketType.Main }) { Amount = amount, Rare = rare, Upgrade = upgrade });

            Service = new GameObject.Services.ItemGrantService.ItemGrantService(
                NullLogger<GameObject.Services.ItemGrantService.ItemGrantService>.Instance,
                items,
                World.WorldConfiguration,
                ItemProvider.Object,
                World.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task GoldIsNotGrantedAsAnItem()
        {
            await new Spec("Gold is refused as an item")
                .When(Granting_WithDesignOrAmount_AndUpgrade_, (short)1046, (short?)null, (byte?)null)
                .Then(TheOutcomeShouldBe_, ItemGrantOutcome.Refused)
                .And(NothingShouldBeCreated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnknownItemIsNotGranted()
        {
            await new Spec("An item the game does not have is refused")
                .When(Granting_WithDesignOrAmount_AndUpgrade_, (short)9999, (short?)null, (byte?)null)
                .Then(TheOutcomeShouldBe_, ItemGrantOutcome.UnknownItem)
                .And(NothingShouldBeCreated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AStackableItemTakesTheAmount()
        {
            await new Spec("A stackable item takes the first value as its amount")
                .When(Granting_WithDesignOrAmount_AndUpgrade_, PotionVNum, (short?)5, (byte?)null)
                .Then(TheOutcomeShouldBe_, ItemGrantOutcome.Granted)
                .And(TheGrantedAmountShouldBe_, (short)5)
                .And(TheItemShouldBeCreatedWith_Amount_Rare_Upgrade_Design_, PotionVNum, (short)5, (sbyte)0, (byte)0, (byte)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheAmountIsCappedAtTheMaximumStack()
        {
            await new Spec("An amount above the maximum stack is capped")
                .When(Granting_WithDesignOrAmount_AndUpgrade_, PotionVNum, (short?)5000, (byte?)null)
                .Then(TheGrantedAmountShouldBe_, (short)999)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AWeaponTakesItsRarityAndUpgrade()
        {
            await new Spec("A weapon reads the first value as rarity and the second as upgrade")
                .When(Granting_WithDesignOrAmount_AndUpgrade_, WeaponVNum, (short?)7, (byte?)10)
                .Then(TheItemShouldBeCreatedWith_Amount_Rare_Upgrade_Design_, WeaponVNum, (short)1, (sbyte)7, (byte)10, (byte)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASpecialistCardTakesItsUpgradeAndDesign()
        {
            await new Spec("A specialist card reads the first value as upgrade and the second as design")
                .When(Granting_WithDesignOrAmount_AndUpgrade_, SpecialistVNum, (short?)12, (byte?)3)
                .Then(TheItemShouldBeCreatedWith_Amount_Rare_Upgrade_Design_, SpecialistVNum, (short)1, (sbyte)0, (byte)12, (byte)3)
                .ExecuteAsync();
        }

        private void Granting_WithDesignOrAmount_AndUpgrade_(short vnum, short? designOrAmount, byte? upgrade) =>
            Result = Service.Grant(Session.Character, vnum, designOrAmount, upgrade);

        private void TheOutcomeShouldBe_(ItemGrantOutcome outcome) => Assert.AreEqual(outcome, Result.Outcome);

        private void TheGrantedAmountShouldBe_(short amount) => Assert.AreEqual(amount, Result.Amount);

        private void NothingShouldBeCreated() =>
            ItemProvider.Verify(x => x.Create(It.IsAny<short>(), It.IsAny<short>(), It.IsAny<sbyte>(), It.IsAny<byte>(), It.IsAny<byte>()),
                Times.Never);

        private void TheItemShouldBeCreatedWith_Amount_Rare_Upgrade_Design_(short vnum, short amount, sbyte rare,
            byte upgrade, byte design) =>
            ItemProvider.Verify(x => x.Create(vnum, amount, rare, upgrade, design), Times.Once);
    }
}
