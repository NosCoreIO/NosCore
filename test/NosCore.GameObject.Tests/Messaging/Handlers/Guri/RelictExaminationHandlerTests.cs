//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD.Steps;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class RelictExaminationHandlerTests
    {
        private const short UnknownRelict = 1878;
        private const int Fee = 10000;
        private const short Reward = 1012;
        private const int Archaeologist = 1;

        private TestWorld World;
        private ClientSession _session;
        private Mock<INpcServiceCatalog> _services;
        private RelictExaminationHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _services = World.Freeze<Mock<INpcServiceCatalog>>();
            _services.Setup(s => s.Offers(It.IsAny<IAliveEntity>(), NpcService.RelictExamination)).Returns(true);
            var rolls = World.Freeze<Mock<IDao<RollGeneratedItemDto, short>>>();
            rolls.Setup(r => r.Where(It.IsAny<Expression<Func<RollGeneratedItemDto, bool>>>())).Returns(new List<RollGeneratedItemDto>
            {
                new() { RollGeneratedItemId = 1, OriginalItemVNum = UnknownRelict, ItemGeneratedVNum = Reward, ItemGeneratedAmount = 1, Probability = 1 },
            });
            _handler = World.Create<RelictExaminationHandler>();
            _session.WithNpcAt(Archaeologist, 1, 1);
        }

        [TestMethod]
        public async Task TheArchaeologistExaminesARelictForItsFee()
        {
            await new Spec("Examining takes the relict and its fee and gives what it held")
                .Given(TheCharacterHoldsAnUnknownRelict)
                .And(TheCharacterHas_Gold, 25000L)
                .And(TheExaminationWindowIsOpen)
                .WhenAsync(ExaminingFor_Gold, Fee)
                .Then(TheCharacterHolds_Relicts, 0)
                .And(TheCharacterHas_GoldLeft, 15000L)
                .And(TheCharacterHolds_Of_, 1, Reward)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NotEnoughGoldKeepsTheRelict()
        {
            await new Spec("A character short of the fee keeps the relict and the gold")
                .Given(TheCharacterHoldsAnUnknownRelict)
                .And(TheCharacterHas_Gold, 9999L)
                .And(TheExaminationWindowIsOpen)
                .WhenAsync(ExaminingFor_Gold, Fee)
                .Then(TheCharacterHolds_Relicts, 1)
                .And(TheCharacterHas_GoldLeft, 9999L)
                .And(TheCharacterIsTold_, Game18NConstString.NotEnoughGold)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACharacterBelowTheRelictsLevelKeepsIt()
        {
            await new Spec("A character below the relict's level is refused")
                .Given(TheCharacterHoldsAnUnknownRelict)
                .And(TheCharacterHas_Gold, 25000L)
                .And(TheCharacterIsLevel_, (byte)59)
                .And(TheExaminationWindowIsOpen)
                .WhenAsync(ExaminingFor_Gold, Fee)
                .Then(TheCharacterHolds_Relicts, 1)
                .And(TheCharacterHas_GoldLeft, 25000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFeeNoRelictCostsExaminesNothing()
        {
            await new Spec("A fee that matches none of the character's relicts does nothing")
                .Given(TheCharacterHoldsAnUnknownRelict)
                .And(TheCharacterHas_Gold, 50000L)
                .And(TheExaminationWindowIsOpen)
                .WhenAsync(ExaminingFor_Gold, 30000)
                .Then(TheCharacterHolds_Relicts, 1)
                .And(TheCharacterHas_GoldLeft, 50000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NothingIsExaminedAwayFromTheArchaeologist()
        {
            await new Spec("Without the archaeologist's window open the relict is left alone")
                .Given(TheCharacterHoldsAnUnknownRelict)
                .And(TheCharacterHas_Gold, 25000L)
                .WhenAsync(ExaminingFor_Gold, Fee)
                .Then(TheCharacterHolds_Relicts, 1)
                .And(TheCharacterHas_GoldLeft, 25000L)
                .ExecuteAsync();
        }

        private void TheCharacterHoldsAnUnknownRelict()
        {
            var item = new Item
            {
                VNum = UnknownRelict,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Special,
                Effect = ItemEffectType.Relict,
                EffectValue = Fee,
                LevelMinimum = 60,
            };
            var instance = new ItemInstanceForTest(UnknownRelict) { Amount = 1, Item = item };
            var relict = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            relict.Slot = 3;
            relict.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[relict.ItemInstanceId] = relict;
            _session.Character.Level = 70;
        }

        private void TheCharacterHas_Gold(long gold) => _session.Character.Gold = gold;

        private void TheCharacterIsLevel_(byte level) => _session.Character.Level = level;

        private void TheExaminationWindowIsOpen() =>
            _session.WithWindowOpenedAt(Archaeologist, NpcDialogWindow.RelictExamination);

        private Task ExaminingFor_Gold(int fee) =>
            _handler.Handle(new GuriPacketReceivedEvent(_session, new NosCore.Packets.ClientPackets.UI.GuriPacket { Type = GuriPacketType.ActVRelic, Argument = fee }));

        private void TheCharacterHolds_Relicts(int count) =>
            Assert.AreEqual(count, _session.Character.InventoryService.CountItem(UnknownRelict));

        private void TheCharacterHas_GoldLeft(long gold) => Assert.AreEqual(gold, _session.Character.Gold);

        private void TheCharacterHolds_Of_(int count, short vnum) =>
            Assert.AreEqual(count, _session.Character.InventoryService.CountItem(vnum));

        private void TheCharacterIsTold_(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<InfoiPacket>().Any(p => p.Message == message));

        private sealed class ItemInstanceForTest(short vnum) : ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
