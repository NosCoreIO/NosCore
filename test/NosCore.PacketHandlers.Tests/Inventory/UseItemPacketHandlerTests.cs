//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class UseItemPacketHandlerTests
    {
        private const short BoxVNum = 1013;
        private const short RewardVNum = 1012;
        private ClientSession Session = null!;
        private IItemGenerationService ItemProvider = null!;
        private UseItemPacketHandler Handler = null!;
        private InlineOrDeferredMessageBus<ItemUsedEvent> Bus = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            ItemProvider = TestHelpers.Instance.GenerateItemProvider();
            var rollDao = new Mock<IDao<RollGeneratedItemDto, short>>();
            rollDao.Setup(x => x.Where(It.IsAny<Expression<Func<RollGeneratedItemDto, bool>>>()))
                .Returns(new List<RollGeneratedItemDto>
                {
                    new()
                    {
                        OriginalItemVNum = BoxVNum,
                        ItemGeneratedVNum = RewardVNum,
                        ItemGeneratedAmount = 1,
                        Probability = 100,
                    },
                });
            var boxHandler = new BoxEffectHandler(ItemProvider, rollDao.Object);
            Bus = new InlineOrDeferredMessageBus<ItemUsedEvent>(evt => boxHandler.Handle(evt));
            Handler = new UseItemPacketHandler(Bus.Object);
        }

        [TestMethod]
        public async Task TheSameBoxUsedTwiceGrantsASingleReward()
        {
            await new Spec("The same box used twice grants a single reward")
                .Given(TheCharacterHoldsOneBox)
                .WhenAsync(TheSameUseIsSentTwiceConcurrently)
                .Then(TheCharacterHolds__, RewardVNum, 1)
                .And(TheCharacterHolds__, BoxVNum, 0)
                .ExecuteAsync();
        }

        private void TheCharacterHoldsOneBox()
        {
            var box = ItemProvider.Create(BoxVNum, 1);
            box.Item = new Item { VNum = BoxVNum, Type = NoscorePocketType.Main, Effect = ItemEffectType.BoxEffect };
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(box, Session.Character.CharacterId), NoscorePocketType.Main, 0);
        }

        private async Task TheSameUseIsSentTwiceConcurrently()
        {
            var packet = new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = 0,
                Mode = 1,
            };
            await Task.WhenAll(Handler.ExecuteAsync(packet, Session), Handler.ExecuteAsync(packet, Session));
            await Bus.DeliverPublishedAsync();
        }

        private void TheCharacterHolds__(short vnum, int amount) =>
            Assert.AreEqual(amount, Session.Character.InventoryService.CountItem(vnum));
    }
}
