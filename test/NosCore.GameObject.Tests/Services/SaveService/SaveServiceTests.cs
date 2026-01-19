//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao;
using NosCore.Data.Dto;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.SaveService
{
    [TestClass]
    public class SaveServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private ClientSession Session = null!;
        private ISaveService Service = null!;
        private IItemGenerationService ItemProvider = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            ItemProvider = TestHelpers.Instance.GenerateItemProvider();

            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                Guid.NewGuid().ToString());
            NosCoreContext ContextBuilder() => new NosCoreContext(optionsBuilder.Options);

            var itemInstanceDao = new Dao<ItemInstance, IItemInstanceDto?, Guid>(Logger, ContextBuilder);
            var inventoryItemInstanceDao = new Dao<Database.Entities.InventoryItemInstance, InventoryItemInstanceDto, Guid>(Logger, ContextBuilder);
            var staticBonusDao = new Dao<StaticBonus, StaticBonusDto, long>(Logger, ContextBuilder);
            var quicklistEntriesDao = new Dao<QuicklistEntry, QuicklistEntryDto, Guid>(Logger, ContextBuilder);
            var titleDao = new Dao<Title, TitleDto, Guid>(Logger, ContextBuilder);
            var characterQuestDao = new Dao<CharacterQuest, CharacterQuestDto, Guid>(Logger, ContextBuilder);

            var minilandService = new Mock<IMinilandService>();
            minilandService.Setup(s => s.GetMiniland(It.IsAny<long>()))
                .Returns(new NosCore.GameObject.Services.MinilandService.Miniland { MinilandMessage = "Test" });

            Service = new GameObject.Services.SaveService.SaveService(
                TestHelpers.Instance.CharacterDao,
                itemInstanceDao,
                inventoryItemInstanceDao,
                TestHelpers.Instance.AccountDao,
                staticBonusDao,
                quicklistEntriesDao,
                TestHelpers.Instance.MinilandDao,
                minilandService.Object,
                titleDao,
                characterQuestDao,
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task SavingCharacterShouldPersistData()
        {
            await new Spec("Saving character should persist data")
                .GivenAsync(CharacterHasModifiedGold)
                .WhenAsync(SavingCharacter)
                .ThenAsync(GoldShouldBePersisted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SavingCharacterShouldPersistInventory()
        {
            await new Spec("Saving character should persist inventory")
                .GivenAsync(CharacterHasItemsInInventory)
                .WhenAsync(SavingCharacter)
                .ThenAsync(InventoryShouldBePersisted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SavingNonCharacterEntityShouldNotThrow()
        {
            await new Spec("Saving non-character entity should not throw")
                .WhenAsync(SavingNonCharacterEntity)
                .Then(NoExceptionShouldBeThrown)
                .ExecuteAsync();
        }

        private bool SaveCompleted;

        private async Task CharacterHasModifiedGold()
        {
            Session.Character.Gold = 999999;
        }

        private async Task CharacterHasItemsInInventory()
        {
            var item = ItemProvider.Create(1012, 50);
            Session.Character.InventoryService.AddItemToPocket(
                GameObject.Services.InventoryService.InventoryItemInstance.Create(item, Session.Character.CharacterId));
        }

        private async Task SavingCharacter()
        {
            await Service.SaveAsync(Session.Character);
            SaveCompleted = true;
        }

        private async Task SavingNonCharacterEntity()
        {
            var mockEntity = new Mock<ComponentEntities.Interfaces.ICharacterEntity>();
            await Service.SaveAsync(mockEntity.Object);
            SaveCompleted = true;
        }

        private async Task GoldShouldBePersisted()
        {
            var character = await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(c =>
                c.CharacterId == Session.Character.CharacterId);
            Assert.IsNotNull(character);
            Assert.AreEqual(999999, character.Gold);
        }

        private async Task InventoryShouldBePersisted()
        {
            var character = await TestHelpers.Instance.CharacterDao.FirstOrDefaultAsync(c =>
                c.CharacterId == Session.Character.CharacterId);
            Assert.IsNotNull(character);
        }

        private void NoExceptionShouldBeThrown()
        {
            Assert.IsTrue(SaveCompleted);
        }
    }
}
