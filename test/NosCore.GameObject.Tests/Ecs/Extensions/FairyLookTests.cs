//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class FairyLookTests
    {
        private const short FairyVNum = 274;
        private const short BoostCardId = 131;

        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;
        private Mock<Wolverine.IMessageBus> _bus;
        private WearableInstance _fairy;

        private static readonly CardDto Booster = new() { CardId = BoostCardId };
        private static readonly IReadOnlyList<BCardDto> BoosterBCards = new[]
        {
            new BCardDto
            {
                Type = BCardEffect.Type96IncreaseFairyElement.Type(),
                SubType = BCardEffect.Type96IncreaseFairyElement.SubType(),
                FirstData = 30,
            },
        };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _bus = World.Freeze<Mock<Wolverine.IMessageBus>>();
            _buffs = World.Create<BuffService>();
            WearFairy();
        }

        [TestMethod]
        public void AnUnboostedFairyKeepsItsOwnLook()
        {
            var pairy = _session.Character.GeneratePairy(_fairy);

            Assert.AreEqual(12, pairy.Morph);
        }

        [TestMethod]
        public async Task ABoostedFairyShowsItsBoostedLook()
        {
            await _buffs.ApplyStaticAsync(_session.Character, Booster, BoosterBCards, Duration.FromHours(1));

            var pairy = _session.Character.GeneratePairy(_fairy);

            Assert.AreEqual(17, pairy.Morph);
            Assert.AreEqual(50, pairy.ElementRate);
        }

        [TestMethod]
        public async Task TheBoostStartingAndEndingBothRefreshTheFairy()
        {
            await _buffs.ApplyStaticAsync(_session.Character, Booster, BoosterBCards, Duration.FromHours(1));
            await _buffs.RemoveAsync(_session.Character, BoostCardId);

            Assert.AreEqual(12, _session.Character.GeneratePairy(_fairy).Morph);
            _bus.Verify(b => b.PublishAsync(It.IsAny<FairyLookChangedEvent>(), It.IsAny<Wolverine.DeliveryOptions>()),
                Times.Exactly(2));
        }

        [TestMethod]
        public void TheInfoCardCountsTheElementTheFairyHasGrown()
        {
            var info = _session.Character.GenerateReqInfo();

            Assert.AreEqual(50, info.ElementRate);
        }

        private void WearFairy()
        {
            var item = new Item
            {
                VNum = FairyVNum,
                Type = NoscorePocketType.Equipment,
                ItemType = ItemType.Jewelery,
                EquipmentSlot = EquipmentType.Fairy,
                Element = ElementType.Fire,
                ElementRate = 20,
                Morph = 12,
            };
            _fairy = new WearableInstance(item, new Mock<ILogger<WearableInstance>>().Object, World.LogLanguageLocalizer)
            {
                ElementRate = 30,
            };
            var worn = InventoryItemInstance.Create(_fairy, _session.Character.CharacterId);
            worn.Slot = (short)EquipmentType.Fairy;
            worn.Type = NoscorePocketType.Wear;
            _session.Character.InventoryService[worn.ItemInstanceId] = worn;
        }
    }
}
