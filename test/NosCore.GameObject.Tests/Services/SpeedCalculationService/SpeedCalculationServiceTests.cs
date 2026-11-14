//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Ecs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Moq;
using NosCore.Algorithm.SpeedService;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.SpeedCalculationService
{
    [TestClass]
    public class SpeedCalculationServiceTests
    {
        private TestWorld World;
        private GameObject.Services.SpeedCalculationService.SpeedCalculationService SpeedCalculationService;
        private Mock<ISpeedService> SpeedService;
        private Mock<IBuffService> Buffs;
        private Mock<ICardCatalog> Catalog;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            SpeedService = World.Freeze<Mock<ISpeedService>>();
            Buffs = World.Freeze<Mock<IBuffService>>();
            Buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(Array.Empty<BuffInstance>());
            Catalog = World.Freeze<Mock<ICardCatalog>>();
            Catalog.Setup(c => c.GetItemBCards(It.IsAny<short>())).Returns(Array.Empty<BCardDto>());
            SpeedCalculationService = World.Create<GameObject.Services.SpeedCalculationService.SpeedCalculationService>();
        }

        [DataTestMethod]
        [DataRow((int)CharacterClassType.Archer)]
        [DataRow((int)CharacterClassType.Mage)]
        [DataRow((int)CharacterClassType.Swordsman)]
        [DataRow((int)CharacterClassType.MartialArtist)]
        [DataRow((int)CharacterClassType.Adventurer)]
        public async Task DefaultSpeedIsClassSpeed(int characterClassInt)
        {
            var characterClass = (CharacterClassType)characterClassInt;
            SpeedService.Setup(x => x.GetSpeed(characterClass)).Returns((byte)characterClassInt);

            var character = (await World.CreateSessionAsync()).Character;
            character.Class = characterClass;

            var speed = SpeedCalculationService.CalculateSpeed(character);
            Assert.AreEqual((byte)characterClassInt, speed);
        }

        [DataTestMethod]
        [DataRow((int)CharacterClassType.Archer)]
        [DataRow((int)CharacterClassType.Mage)]
        [DataRow((int)CharacterClassType.Swordsman)]
        [DataRow((int)CharacterClassType.MartialArtist)]
        [DataRow((int)CharacterClassType.Adventurer)]
        public async Task VehicleSpeedOverridesDefaultSpeed(int characterClassInt)
        {
            var characterClass = (CharacterClassType)characterClassInt;
            SpeedService.Setup(x => x.GetSpeed(characterClass)).Returns((byte)characterClassInt);

            var character = (await World.CreateSessionAsync()).Character;
            character.Class = characterClass;
            // Both, because a mount sets both. Setting only VehicleSpeed described a state that
            // cannot happen, and the old null check happened to answer it correctly.
            character.IsVehicled = true;
            character.VehicleSpeed = 50;

            var speed = SpeedCalculationService.CalculateSpeed(character);
            Assert.AreEqual(50, speed);
        }

        // On foot VehicleSpeed is 0, not null - the component declares it as a plain byte. Reading
        // it whenever it "is not null" therefore answered 0 for everyone, and it went unnoticed
        // because nothing called this service.
        [TestMethod]
        public async Task OnFootTheVehicleSpeedIsIgnoredRatherThanReturnedAsZero()
        {
            SpeedService.Setup(x => x.GetSpeed(CharacterClassType.Archer)).Returns((byte)12);

            var character = (await World.CreateSessionAsync()).Character;
            character.Class = CharacterClassType.Archer;
            character.IsVehicled = false;
            character.VehicleSpeed = (byte)0;

            Assert.AreEqual(12, SpeedCalculationService.CalculateSpeed(character));
        }

        [TestMethod]
        public async Task HasteAddsItsSpeedAndASlowTakesItsAway()
        {
            SpeedService.Setup(x => x.GetSpeed(CharacterClassType.Archer)).Returns((byte)12);
            var character = (await World.CreateSessionAsync()).Character;
            character.Class = CharacterClassType.Archer;
            Buffs.Setup(b => b.GetActiveBuffs(character)).Returns(new[]
            {
                Buff(28, Move(BCardEffect.MoveMovementSpeedIncreased, 2)),
                Buff(70, Move(BCardEffect.MoveMovementSpeedDecreased, 3)),
            });

            Assert.AreEqual(11, SpeedCalculationService.CalculateSpeed(character));
        }

        [TestMethod]
        public async Task ARootedCharacterHasNoSpeed()
        {
            SpeedService.Setup(x => x.GetSpeed(CharacterClassType.Archer)).Returns((byte)12);
            var character = (await World.CreateSessionAsync()).Character;
            character.Class = CharacterClassType.Archer;
            Buffs.Setup(b => b.HasEffect(character, BCardEffect.MoveMovementImpossible)).Returns(true);

            Assert.AreEqual(0, SpeedCalculationService.CalculateSpeed(character));
        }

        [TestMethod]
        public async Task AWornCostumeWithASpeedBonusRaisesTheSpeed()
        {
            const short costumeVNum = 4245;
            SpeedService.Setup(x => x.GetSpeed(CharacterClassType.Archer)).Returns((byte)12);
            Catalog.Setup(c => c.GetItemBCards(costumeVNum))
                .Returns(new[] { Move(BCardEffect.MoveMovementSpeedIncreased, 2) });
            var character = (await World.CreateSessionAsync()).Character;
            character.Class = CharacterClassType.Archer;
            var item = new Item
            {
                VNum = costumeVNum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.CostumeSuit,
            };
            var costume = InventoryItemInstance.Create(
                new WearableInstance(item, new Mock<ILogger<WearableInstance>>().Object, World.LogLanguageLocalizer)
                {
                    Amount = 1,
                    ItemVNum = costumeVNum,
                }, character.CharacterId);
            costume.Slot = (short)EquipmentType.CostumeSuit;
            costume.Type = NoscorePocketType.Wear;
            character.InventoryService[costume.ItemInstanceId] = costume;

            Assert.AreEqual(14, SpeedCalculationService.CalculateSpeed(character));
        }

        private static BCardDto Move(BCardEffect effect, int value) =>
            new() { Type = (byte)((short)effect / 100), SubType = (byte)((short)effect % 100), FirstData = value };

        private static BuffInstance Buff(short cardId, BCardDto bCard) =>
            new(cardId, BuffType.Good, null, NodaTime.Instant.MinValue, NodaTime.Instant.MaxValue, new[] { bCard });

        [TestMethod]
        public void DefaultMonsterSpeedIsNpcMonsterSpeed()
        {
            new Spec("Default monster speed is npc monster speed")
                .Then(MonsterSpeedShouldMatchNpcMonsterDto)
                .Execute();
        }

        private void MonsterSpeedShouldMatchNpcMonsterDto()
        {
            var charMock = new Mock<INonPlayableEntity>();
            charMock.SetupGet(x => x.NpcMonster).Returns(new NpcMonsterDto
            {
                Speed = 50
            });
            var speed = SpeedCalculationService.CalculateSpeed(charMock.Object);
            Assert.AreEqual(50, speed);
        }
    }
}
