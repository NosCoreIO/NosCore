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
using NosCore.Algorithm.SpPerfectionService;
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
    public class SpPerfectionOperationTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IRandomNumberSource> _random;
        private SpPerfectionOperation _operation;
        private SpecialistInstance _card;
        private IReadOnlyList<IPacket> _result = Array.Empty<IPacket>();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _random = new Mock<IRandomNumberSource>();
            _operation = new SpPerfectionOperation(_random.Object, World.GameLanguageLocalizer,
                new SpPerfectionService(), UpgradeMaterials.Catalog());
            _session.Character.Gold = 10_000_000;
        }

        [TestMethod]
        public async Task ASuccessRaisesThePerfectionAndTheRolledStat()
        {
            PlaceCard(MaterialType.SmallRubyOfCompletion, perfection: 0);
            AddStones(UpgradeMaterials.VNum(MaterialType.SmallRubyOfCompletion));
            Roll(0.0);

            await PerfectAsync();

            Assert.AreEqual((byte)1, _card.SpStoneUpgrade);
            Assert.AreEqual((byte)1, _card.SpDamage);
            Assert.AreEqual(100 - 1, Count(UpgradeMaterials.VNum(MaterialType.SmallRubyOfCompletion)));
            Assert.AreEqual(10_000_000 - 5_000, _session.Character.Gold);
            var msgi = Msgi();
            Assert.AreEqual(Game18NConstString.PerfectingSpecialistSuccessful, msgi.Message);
            Assert.AreEqual((long)Game18NConstString.Attack, msgi.Game18NArguments[0]);
            Assert.AreEqual(1L, msgi.Game18NArguments[1]);
            Assert.IsFalse(_result.OfType<SayiPacket>().Any());
        }

        [TestMethod]
        public async Task AFailureSpendsTheStonesAndKeepsTheCard()
        {
            PlaceCard(MaterialType.SmallRubyOfCompletion, perfection: 0);
            AddStones(UpgradeMaterials.VNum(MaterialType.SmallRubyOfCompletion));
            Roll(0.6);

            await PerfectAsync();

            Assert.AreEqual((byte)0, _card.SpStoneUpgrade);
            Assert.AreEqual((byte)0, _card.SpDamage);
            Assert.AreEqual(100 - 1, Count(UpgradeMaterials.VNum(MaterialType.SmallRubyOfCompletion)));
            Assert.AreEqual(10_000_000 - 5_000, _session.Character.Gold);
            Assert.AreEqual(Game18NConstString.PerfectingSpecialistFailed, Msgi().Message);
        }

        [TestMethod]
        public async Task TheLastFaceOfTheRollIsShadowResistance()
        {
            PlaceCard(MaterialType.SmallObsidianOfCompletion, perfection: 0);
            AddStones(UpgradeMaterials.VNum(MaterialType.SmallObsidianOfCompletion));
            _random.SetupSequence(r => r.NextDouble()).Returns(0.0).Returns(0.0).Returns(0.99);

            await PerfectAsync();

            Assert.AreEqual((byte)1, _card.SpDark);
            Assert.AreEqual(100 - 1, Count(UpgradeMaterials.VNum(MaterialType.SmallObsidianOfCompletion)));
        }

        [TestMethod]
        public async Task FromPerfectionTwentyTwoStonesAndMoreGoldAreUsed()
        {
            PlaceCard(MaterialType.SmallSapphireOfCompletion, perfection: 20);
            AddStones(UpgradeMaterials.VNum(MaterialType.SmallSapphireOfCompletion));
            Roll(0.99);

            await PerfectAsync();

            Assert.AreEqual(100 - 2, Count(UpgradeMaterials.VNum(MaterialType.SmallSapphireOfCompletion)));
            Assert.AreEqual(10_000_000 - 10_000, _session.Character.Gold);
        }

        [TestMethod]
        public async Task TheCardsOwnStoneIsRequired()
        {
            PlaceCard(MaterialType.SmallRubyOfCompletion, perfection: 0);
            AddStones(UpgradeMaterials.VNum(MaterialType.SmallSapphireOfCompletion));

            await PerfectAsync();

            Assert.AreEqual(Game18NConstString.NotEnoughIngredients, _result.OfType<InfoiPacket>().Single().Message);
            Assert.AreEqual((byte)0, _card.SpStoneUpgrade);
            Assert.AreEqual(10_000_000, _session.Character.Gold);
        }

        [TestMethod]
        public async Task ACardWithoutAStoneCannotBePerfected()
        {
            PlaceCard(MaterialType.None, perfection: 0);
            AddStones(UpgradeMaterials.VNum(MaterialType.SmallRubyOfCompletion));

            await PerfectAsync();

            Assert.IsFalse(_result.Any());
            Assert.AreEqual(100, Count(UpgradeMaterials.VNum(MaterialType.SmallRubyOfCompletion)));
        }

        [TestMethod]
        public async Task ADestroyedCardIsRefused()
        {
            PlaceCard(MaterialType.SmallRubyOfCompletion, perfection: 0);
            _card.Rare = SpUpgradeOperationBase.DestroyedSoulRare;
            AddStones(UpgradeMaterials.VNum(MaterialType.SmallRubyOfCompletion));

            await PerfectAsync();

            Assert.AreEqual(Game18NConstString.CantUseBecauseSoulDestroyed, Sayi().Message);
            Assert.AreEqual(100, Count(UpgradeMaterials.VNum(MaterialType.SmallRubyOfCompletion)));
        }

        [TestMethod]
        public async Task AFullyPerfectedCardIsRefused()
        {
            PlaceCard(MaterialType.SmallRubyOfCompletion, perfection: 100);
            AddStones(UpgradeMaterials.VNum(MaterialType.SmallRubyOfCompletion));

            await PerfectAsync();

            Assert.AreEqual(Game18NConstString.MaximumUpgradeReached, Sayi().Message);
        }

        private void PlaceCard(MaterialType stone, byte perfection)
        {
            _card = new SpecialistInstance(new Item
            {
                VNum = 901, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist,
                EquipmentSlot = EquipmentType.Sp, PerfectionMaterial = stone,
            })
            {
                Amount = 1,
                SpLevel = 50,
                SpStoneUpgrade = perfection,
            };
            var inst = InventoryItemInstance.Create(_card, _session.Character.CharacterId);
            inst.Slot = 0;
            inst.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[inst.ItemInstanceId] = inst;
        }

        private void AddStones(short vnum)
        {
            var inst = InventoryItemInstance.Create(
                new ItemInstance(new Item { VNum = vnum, Type = NoscorePocketType.Etc }) { Amount = 100 },
                _session.Character.CharacterId);
            inst.Slot = 0;
            inst.Type = NoscorePocketType.Etc;
            _session.Character.InventoryService[inst.ItemInstanceId] = inst;
        }

        private void Roll(double value) => _random.Setup(r => r.NextDouble()).Returns(value);

        private async Task PerfectAsync() =>
            _result = await _operation.ExecuteAsync(_session, new UpgradePacket
            {
                UpgradeType = _operation.Kind,
                InventoryType = PocketType.Equipment,
                Slot = 0,
            });

        private int Count(short vnum) => _session.Character.InventoryService.CountItem(vnum);

        private MsgiPacket Msgi() => _result.OfType<MsgiPacket>().Single();

        private SayiPacket Sayi() => _result.OfType<SayiPacket>().Single();
    }
}
