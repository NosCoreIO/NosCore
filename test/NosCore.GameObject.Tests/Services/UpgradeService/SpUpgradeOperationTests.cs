//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.SpUpgradeService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.UpgradeService
{
    [TestClass]
    public class SpUpgradeOperationTests
    {
        private ClientSession _session;
        private Mock<IRandomNumberSource> _random;
        private SpUpgradeOperation _unprotected;
        private SpUpgradeProtectedOperation _protected;
        private SpecialistInstance _card;
        private IReadOnlyList<IPacket> _result = Array.Empty<IPacket>();

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _random = new Mock<IRandomNumberSource>();
            _unprotected = new SpUpgradeOperation(_random.Object, TestHelpers.Instance.GameLanguageLocalizer,
                new SpUpgradeService());
            _protected = new SpUpgradeProtectedOperation(_random.Object, TestHelpers.Instance.GameLanguageLocalizer,
                new SpUpgradeService());
            _session.Character.Gold = 10_000_000;
        }

        [TestMethod]
        public async Task ASuccessAddsALevelAndSpendsTheSouls()
        {
            PlaceCard(upgrade: 0, spLevel: 30);
            AddMaterials();
            Roll(0.99);

            await UpgradeAsync(_unprotected);

            Assert.AreEqual((byte)1, _card.Upgrade);
            Assert.AreEqual(100 - 3, Count(SpUpgradeOperationBase.FeatherVNum));
            Assert.AreEqual(100 - 1, Count(SpUpgradeOperationBase.FullMoonVNum));
            Assert.AreEqual(100 - 2, Count(SpUpgradeOperationBase.GreenSoulVNum));
            Assert.AreEqual(10_000_000 - 200_000, _session.Character.Gold);
            Assert.AreEqual(Game18NConstString.UpgradeSuccessful, Msgi().Message);
        }

        [TestMethod]
        public async Task AFailureKeepsTheLevelAndTheSouls()
        {
            PlaceCard(upgrade: 0, spLevel: 30);
            AddMaterials();
            Roll(0.10);

            await UpgradeAsync(_unprotected);

            Assert.AreEqual((byte)0, _card.Upgrade);
            Assert.AreEqual((sbyte)0, _card.Rare);
            Assert.AreEqual(100 - 3, Count(SpUpgradeOperationBase.FeatherVNum));
            Assert.AreEqual(100, Count(SpUpgradeOperationBase.GreenSoulVNum));
            Assert.AreEqual(Game18NConstString.SpecialistUpgradeFailed, Msgi().Message);
        }

        [TestMethod]
        public async Task ARollUnderTheDestructionRateDestroysTheSoul()
        {
            PlaceCard(upgrade: 5, spLevel: 50);
            AddMaterials();
            Roll(0.10);

            await UpgradeAsync(_unprotected);

            Assert.AreEqual((byte)5, _card.Upgrade);
            Assert.AreEqual(SpUpgradeOperationBase.DestroyedSoulRare, _card.Rare);
            Assert.AreEqual(Game18NConstString.SpecialistSouldDestroyed, Msgi().Message);
        }

        [TestMethod]
        public async Task AProtectionScrollSavesTheSoul()
        {
            PlaceCard(upgrade: 5, spLevel: 50);
            AddMaterials();
            Roll(0.10);

            await UpgradeAsync(_protected);

            Assert.AreEqual((sbyte)0, _card.Rare);
            Assert.AreEqual(100 - 1, Count(SpUpgradeOperationBase.BlueScrollVNum));
            Assert.AreEqual(Game18NConstString.SpecialistSouldProtected, Msgi().Message);
        }

        [TestMethod]
        public async Task FromPlusTenTheRedScrollAndBlueSoulsAreUsed()
        {
            PlaceCard(upgrade: 10, spLevel: 60);
            AddMaterials();
            Roll(0.99);

            await UpgradeAsync(_protected);

            Assert.AreEqual((byte)11, _card.Upgrade);
            Assert.AreEqual(100 - 1, Count(SpUpgradeOperationBase.RedScrollVNum));
            Assert.AreEqual(100, Count(SpUpgradeOperationBase.BlueScrollVNum));
            Assert.AreEqual(100 - 1, Count(SpUpgradeOperationBase.BlueSoulVNum));
        }

        [TestMethod]
        public async Task ACardBelowTheJobLevelIsRefused()
        {
            PlaceCard(upgrade: 5, spLevel: 40);
            AddMaterials();

            await UpgradeAsync(_unprotected);

            Assert.AreEqual(Game18NConstString.SpecialistCardRequireLevelFourtyOne, Sayi().Message);
            Assert.AreEqual(100, Count(SpUpgradeOperationBase.FeatherVNum));
        }

        [TestMethod]
        public async Task MissingFeathersAreNamed()
        {
            PlaceCard(upgrade: 0, spLevel: 30);

            await UpgradeAsync(_unprotected);

            Assert.AreEqual(Game18NConstString.NotEnoughAngelFeathers, Sayi().Message);
        }

        [TestMethod]
        public async Task ADestroyedCardIsRefused()
        {
            PlaceCard(upgrade: 3, spLevel: 30);
            _card.Rare = SpUpgradeOperationBase.DestroyedSoulRare;
            AddMaterials();

            await UpgradeAsync(_unprotected);

            Assert.AreEqual(Game18NConstString.CantUseBecauseSoulDestroyed, Sayi().Message);
        }

        [TestMethod]
        public async Task APlusTwentyCardIsRefused()
        {
            PlaceCard(upgrade: 20, spLevel: 99);
            AddMaterials();

            await UpgradeAsync(_unprotected);

            Assert.AreEqual(Game18NConstString.MaximumUpgradeReached, Sayi().Message);
        }

        [TestMethod]
        public async Task FromPlusFifteenACardSpendsDragonGemsInsteadOfSouls()
        {
            PlaceCard(upgrade: 15, spLevel: 99);
            AddMaterials();
            Roll(0.999);

            await UpgradeAsync(_unprotected);

            Assert.AreEqual((byte)16, _card.Upgrade);
            Assert.AreEqual(100 - 2, Count(SpUpgradeOperationBase.DragonGemVNum));
            Assert.AreEqual(100, Count(SpUpgradeOperationBase.BlueSoulVNum));
            Assert.AreEqual(100, Count(SpUpgradeOperationBase.ShardVNum));
        }

        [TestMethod]
        public async Task FromPlusFifteenACardOfAnotherDimensionAlsoSpendsShards()
        {
            PlaceCard(upgrade: 15, spLevel: 99, morph: 55);
            AddMaterials();
            Roll(0.999);

            await UpgradeAsync(_unprotected);

            Assert.AreEqual((byte)16, _card.Upgrade);
            Assert.AreEqual(100 - 2, Count(SpUpgradeOperationBase.DragonGemVNum));
            Assert.AreEqual(100 - 1, Count(SpUpgradeOperationBase.ShardVNum));
        }

        private void PlaceCard(byte upgrade, byte spLevel, short morph = 1)
        {
            _card = new SpecialistInstance(new Item
            {
                VNum = 901, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist,
                EquipmentSlot = EquipmentType.Sp, Morph = morph,
            })
            {
                Amount = 1,
                Upgrade = upgrade,
                SpLevel = spLevel,
            };
            var inst = InventoryItemInstance.Create(_card, _session.Character.CharacterId);
            inst.Slot = 0;
            inst.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[inst.ItemInstanceId] = inst;
        }

        private void AddMaterials()
        {
            short slot = 0;
            foreach (var vnum in new[]
                     {
                         SpUpgradeOperationBase.FeatherVNum, SpUpgradeOperationBase.FullMoonVNum,
                         SpUpgradeOperationBase.GreenSoulVNum, SpUpgradeOperationBase.RedSoulVNum,
                         SpUpgradeOperationBase.BlueSoulVNum, SpUpgradeOperationBase.BlueScrollVNum,
                         SpUpgradeOperationBase.RedScrollVNum, SpUpgradeOperationBase.DragonGemVNum,
                         SpUpgradeOperationBase.ShardVNum,
                     })
            {
                var inst = InventoryItemInstance.Create(
                    new ItemInstance(new Item { VNum = vnum, Type = NoscorePocketType.Main }) { Amount = 100 },
                    _session.Character.CharacterId);
                inst.Slot = slot++;
                inst.Type = NoscorePocketType.Main;
                _session.Character.InventoryService[inst.ItemInstanceId] = inst;
            }
        }

        private void Roll(double value) => _random.Setup(r => r.NextDouble()).Returns(value);

        private async Task UpgradeAsync(IUpgradeOperation operation) =>
            _result = await operation.ExecuteAsync(_session, new UpgradePacket
            {
                UpgradeType = operation.Kind,
                InventoryType = PocketType.Equipment,
                Slot = 0,
            });

        private int Count(short vnum) => _session.Character.InventoryService.CountItem(vnum);

        private MsgiPacket Msgi() => _result.OfType<MsgiPacket>().Single();

        private SayiPacket Sayi() => _result.OfType<SayiPacket>().Single();
    }
}
