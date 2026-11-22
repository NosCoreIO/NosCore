//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SkillService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class PassiveSkillBookHandlerTests
    {
        private const short BookSkill = 1234;
        private TestWorld World;
        private ClientSession _session;
        private Mock<ISkillService> _skillService;
        private PassiveSkillBookHandler _handler;
        private InventoryItemInstance _book;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _skillService = World.Freeze<Mock<ISkillService>>();
            _handler = World.Create<PassiveSkillBookHandler>();
        }

        [TestMethod]
        public async Task ALearnedBookIsUsedUp()
        {
            PlaceBook(ItemEffectType.PassiveSkillBook);
            _skillService.Setup(s => s.LearnFromBookAsync(It.IsAny<PlayerComponentBundle>(), BookSkill)).ReturnsAsync(true);

            await UseAsync();

            _skillService.Verify(s => s.LearnFromBookAsync(It.IsAny<PlayerComponentBundle>(), BookSkill), Times.Once);
            Assert.AreEqual(0, BooksLeft());
        }

        [TestMethod]
        public async Task ABookThatCannotBeLearnedIsKept()
        {
            PlaceBook(ItemEffectType.PassiveSkillBook);
            _skillService.Setup(s => s.LearnFromBookAsync(It.IsAny<PlayerComponentBundle>(), BookSkill)).ReturnsAsync(false);

            await UseAsync();

            Assert.AreEqual(1, BooksLeft());
        }

        [TestMethod]
        public async Task AnItemWithoutTheBookEffectTeachesNothing()
        {
            PlaceBook(ItemEffectType.NoEffect);

            await UseAsync();

            _skillService.Verify(s => s.LearnFromBookAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<short>()), Times.Never);
            Assert.AreEqual(1, BooksLeft());
        }

        private void PlaceBook(ItemEffectType effect)
        {
            var book = new ItemInstance(new Item
            {
                VNum = 1, Type = NoscorePocketType.Main, ItemType = ItemType.Special, Effect = effect, EffectValue = BookSkill,
            })
            {
                Amount = 1,
            };
            _book = InventoryItemInstance.Create(book, _session.Character.CharacterId);
            _book.Slot = 0;
            _book.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_book.ItemInstanceId] = _book;
        }

        private int BooksLeft() => _session.Character.InventoryService.CountItem(_book.ItemInstance.ItemVNum);

        private Task UseAsync() =>
            _handler.Handle(new ItemUsedEvent(_session, _book, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
            }));
    }
}
