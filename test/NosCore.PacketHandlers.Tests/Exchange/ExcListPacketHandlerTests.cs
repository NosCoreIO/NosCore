//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Exchange;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Exchange
{
    [TestClass]
    public class ExcListPacketHandlerTests
    {
        private ExcListPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private ClientSession TargetSession = null!;
        private Mock<IExchangeService> ExchangeService = null!;
        private ItemGenerationService ItemProvider = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            ExchangeService = new Mock<IExchangeService>();

            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Main, VNum = 1012, IsTradable = true },
                new Item { Type = NoscorePocketType.Main, VNum = 1013, IsTradable = false }
            };
            ItemProvider = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                    new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()),
                Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Handler = new ExcListPacketHandler(
                ExchangeService.Object,
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task SettingGoldMoreThanAvailableShouldBeIgnored()
        {
            await new Spec("Setting gold more than available should be ignored")
                .Given(CharacterHasNoGold)
                .WhenAsync(SettingGoldInExchange)
                .Then(ExchangeGoldShouldNotBeSet)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingNonTradableItemShouldCloseExchange()
        {
            await new Spec("Adding non tradable item should close exchange")
                .Given(CharacterHasNonTradableItem)
                .And(ExchangeIsOpenWithTarget)
                .WhenAsync(AddingNonTradableItemToExchange)
                .Then(ExchangeShouldBeClosed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingTradableItemShouldSucceed()
        {
            await new Spec("Adding tradable item should succeed")
                .Given(CharacterHasTradableItem)
                .And(ExchangeIsOpenWithTarget)
                .WhenAsync(AddingTradableItemToExchange)
                .Then(ItemShouldBeAddedToExchange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SettingValidGoldShouldUpdateExchange()
        {
            await new Spec("Setting valid gold should update exchange")
                .Given(CharacterHasGold)
                .And(ExchangeIsOpenWithTarget)
                .WhenAsync(SettingGoldInExchange)
                .Then(ExchangeGoldShouldBeSet)
                .ExecuteAsync();
        }

        private void CharacterHasNoGold()
        {
            Session.Character.Gold = 0;
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void CharacterHasGold()
        {
            Session.Character.Gold = 10000;
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void CharacterHasNonTradableItem()
        {
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(1013, 1), 0),
                NoscorePocketType.Main, 0);
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void CharacterHasTradableItem()
        {
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(1012, 10), 0),
                NoscorePocketType.Main, 0);
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void ExchangeIsOpenWithTarget()
        {
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
            ExchangeService.Setup(x => x.GetTargetId(Session.Character.VisualId))
                .Returns(TargetSession.Character.VisualId);
            ExchangeService.Setup(x => x.CloseExchange(It.IsAny<long>(), It.IsAny<ExchangeResultType>()))
                .Returns(new ExcClosePacket { Type = ExchangeResultType.Failure });
        }

        private async Task SettingGoldInExchange()
        {
            await Handler.ExecuteAsync(new ExcListPacket
            {
                Gold = 5000,
                BankGold = 0,
                SubPackets = new List<ExcListSubPacket?>()
            }, Session);
        }

        private async Task AddingNonTradableItemToExchange()
        {
            await Handler.ExecuteAsync(new ExcListPacket
            {
                Gold = 0,
                BankGold = 0,
                SubPackets = new List<ExcListSubPacket?>
                {
                    new() { Slot = 0, PocketType = PocketType.Main, Amount = 1 }
                }
            }, Session);
        }

        private async Task AddingTradableItemToExchange()
        {
            await Handler.ExecuteAsync(new ExcListPacket
            {
                Gold = 0,
                BankGold = 0,
                SubPackets = new List<ExcListSubPacket?>
                {
                    new() { Slot = 0, PocketType = PocketType.Main, Amount = 5 }
                }
            }, Session);
        }

        private void ExchangeGoldShouldNotBeSet()
        {
            ExchangeService.Verify(x => x.SetGold(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        private void ExchangeGoldShouldBeSet()
        {
            ExchangeService.Verify(x => x.SetGold(Session.Character.CharacterId, 5000, 0), Times.Once);
        }

        private void ExchangeShouldBeClosed()
        {
            ExchangeService.Verify(x => x.CloseExchange(Session.Character.VisualId, ExchangeResultType.Failure), Times.Once);
        }

        private void ItemShouldBeAddedToExchange()
        {
            ExchangeService.Verify(x => x.AddItems(Session.Character.CharacterId, It.IsAny<InventoryItemInstance>(), 5), Times.Once);
        }
    }
}
