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
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
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
        private TestWorld World;
        private const short BoxVNum = 1013;
        private const short RewardVNum = 1012;
        private ClientSession Session;
        private IItemGenerationService ItemProvider;
        private UseItemPacketHandler Handler;
        private InlineGameEvents<ItemUsedEvent> GameEvents;
        private Mock<IBuffService> Buffs;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            ItemProvider = World.GenerateItemProvider();
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
            World.Inject(rollDao.Object);
            var boxHandler = World.Create<BoxEffectHandler>();
            GameEvents = new InlineGameEvents<ItemUsedEvent>(evt => boxHandler.Handle(evt));
            Buffs = new Mock<IBuffService>();
            Handler = new UseItemPacketHandler(GameEvents.Object, Buffs.Object);
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

        [TestMethod]
        public async Task AnEffectThatForbidsItemsKeepsTheBoxClosed()
        {
            await new Spec("An effect that forbids items keeps the box closed")
                .Given(TheCharacterHoldsOneBox)
                .And(TheCharacterCarriesAnEffectThatForbidsItems)
                .WhenAsync(TheBoxIsUsed)
                .Then(TheCharacterHolds__, RewardVNum, 0)
                .And(TheCharacterHolds__, BoxVNum, 1)
                .ExecuteAsync();
        }

        private void TheCharacterCarriesAnEffectThatForbidsItems() =>
            Buffs.Setup(b => b.HasEffect(It.IsAny<IAliveEntity>(), BCardEffect.TimeCircleSkillsItemCannotBeUsed)).Returns(true);

        private Task TheBoxIsUsed() => Handler.ExecuteAsync(UseOfSlotZero(), Session);

        private void TheCharacterHoldsOneBox()
        {
            var box = ItemProvider.Create(BoxVNum, 1);
            box.Item = new Item { VNum = BoxVNum, Type = NoscorePocketType.Main, Effect = ItemEffectType.BoxEffect };
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(box, Session.Character.CharacterId), NoscorePocketType.Main, 0);
        }

        private async Task TheSameUseIsSentTwiceConcurrently()
        {
            var packet = UseOfSlotZero();
            await Task.WhenAll(Handler.ExecuteAsync(packet, Session), Handler.ExecuteAsync(packet, Session));
        }

        private UseItemPacket UseOfSlotZero() => new()
        {
            VisualType = VisualType.Player,
            VisualId = Session.Character.CharacterId,
            Type = PocketType.Main,
            Slot = 0,
            Mode = 1,
        };

        private void TheCharacterHolds__(short vnum, int amount) =>
            Assert.AreEqual(amount, Session.Character.InventoryService.CountItem(vnum));
    }
}
