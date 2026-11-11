//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ProductionService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class ProductionScrollHandlerTests
    {
        private const short Scroll = 1014;
        private ClientSession _session;
        private IItemGenerationService _itemProvider;
        private Mock<IProductionService> _productionService;
        private InventoryItemInstance _item;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _itemProvider = TestHelpers.Instance.GenerateItemProvider();
            _productionService = new Mock<IProductionService>();
        }

        [TestMethod]
        public async Task UsingAProductionScrollOpensItsWindow()
        {
            await new Spec("Using a production scroll opens the production window for that scroll")
                .Given(TheCharacterHas_, Scroll)
                .WhenAsync(UsingTheItem)
                .Then(TheScrollWindowShouldOpenFor_, Scroll)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OtherItemsAreIgnored()
        {
            await new Spec("An item without the production effect opens nothing")
                .Given(TheCharacterHas_, (short)1015)
                .WhenAsync(UsingTheItem)
                .Then(NoScrollWindowShouldOpen)
                .ExecuteAsync();
        }

        private void TheCharacterHas_(short vnum) =>
            _item = _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_itemProvider.Create(vnum, 1), _session.Character.CharacterId)).NotNull()[0];

        private Task UsingTheItem() =>
            new ProductionScrollHandler(_productionService.Object).Handle(new ItemUsedEvent(_session, _item,
                new UseItemPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = _session.Character.CharacterId,
                    Type = PocketType.Main,
                    Slot = _item.Slot
                }));

        private void TheScrollWindowShouldOpenFor_(short vnum) =>
            _productionService.Verify(s => s.OpenScrollAsync(_session, vnum), Times.Once);

        private void NoScrollWindowShouldOpen() =>
            _productionService.Verify(s => s.OpenScrollAsync(It.IsAny<ClientSession>(), It.IsAny<short>()), Times.Never);
    }
}
