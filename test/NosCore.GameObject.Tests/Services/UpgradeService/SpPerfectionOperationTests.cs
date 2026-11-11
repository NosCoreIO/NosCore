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
        private ClientSession _session;
        private Mock<IRandomNumberSource> _random;
        private SpPerfectionOperation _operation;
        private SpecialistInstance _card;
        private IReadOnlyList<IPacket> _result = Array.Empty<IPacket>();

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _random = new Mock<IRandomNumberSource>();
            _operation = new SpPerfectionOperation(_random.Object, TestHelpers.Instance.GameLanguageLocalizer,
                new SpPerfectionService());
            _session.Character.Gold = 10_000_000;
        }

        [TestMethod]
        public async Task ASuccessRaisesThePerfectionAndTheRolledStat()
        {
            PlaceCard(morph: 2, perfection: 0);
            AddStones(SpPerfectionOperation.SmallRubyVNum);
            Roll(0.0);

            await PerfectAsync();

            Assert.AreEqual((byte)1, _card.SpStoneUpgrade);
            Assert.AreEqual((byte)1, _card.SpDamage);
            Assert.AreEqual(100 - 1, Count(SpPerfectionOperation.SmallRubyVNum));
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
            PlaceCard(morph: 2, perfection: 0);
            AddStones(SpPerfectionOperation.SmallRubyVNum);
            Roll(0.6);

            await PerfectAsync();

            Assert.AreEqual((byte)0, _card.SpStoneUpgrade);
            Assert.AreEqual((byte)0, _card.SpDamage);
            Assert.AreEqual(100 - 1, Count(SpPerfectionOperation.SmallRubyVNum));
            Assert.AreEqual(10_000_000 - 5_000, _session.Character.Gold);
            Assert.AreEqual(Game18NConstString.PerfectingSpecialistFailed, Msgi().Message);
        }

        [TestMethod]
        public async Task TheLastFaceOfTheRollIsShadowResistance()
        {
            PlaceCard(morph: 5, perfection: 0);
            AddStones(SpPerfectionOperation.SmallObsidianVNum);
            _random.SetupSequence(r => r.NextDouble()).Returns(0.0).Returns(0.0).Returns(0.99);

            await PerfectAsync();

            Assert.AreEqual((byte)1, _card.SpDark);
            Assert.AreEqual(100 - 1, Count(SpPerfectionOperation.SmallObsidianVNum));
        }

        [TestMethod]
        public async Task FromPerfectionTwentyTwoStonesAndMoreGoldAreUsed()
        {
            PlaceCard(morph: 3, perfection: 20);
            AddStones(SpPerfectionOperation.SmallSapphireVNum);
            Roll(0.99);

            await PerfectAsync();

            Assert.AreEqual(100 - 2, Count(SpPerfectionOperation.SmallSapphireVNum));
            Assert.AreEqual(10_000_000 - 10_000, _session.Character.Gold);
        }

        [TestMethod]
        public async Task TheCardsOwnStoneIsRequired()
        {
            PlaceCard(morph: 2, perfection: 0);
            AddStones(SpPerfectionOperation.SmallSapphireVNum);

            await PerfectAsync();

            Assert.AreEqual(Game18NConstString.NotEnoughIngredients, _result.OfType<InfoiPacket>().Single().Message);
            Assert.AreEqual((byte)0, _card.SpStoneUpgrade);
            Assert.AreEqual(10_000_000, _session.Character.Gold);
        }

        [TestMethod]
        public async Task ADestroyedCardIsRefused()
        {
            PlaceCard(morph: 2, perfection: 0);
            _card.Rare = SpUpgradeOperationBase.DestroyedSoulRare;
            AddStones(SpPerfectionOperation.SmallRubyVNum);

            await PerfectAsync();

            Assert.AreEqual(Game18NConstString.CantUseBecauseSoulDestroyed, Sayi().Message);
            Assert.AreEqual(100, Count(SpPerfectionOperation.SmallRubyVNum));
        }

        [TestMethod]
        public async Task AFullyPerfectedCardIsRefused()
        {
            PlaceCard(morph: 2, perfection: 100);
            AddStones(SpPerfectionOperation.SmallRubyVNum);

            await PerfectAsync();

            Assert.AreEqual(Game18NConstString.MaximumUpgradeReached, Sayi().Message);
        }

        private void PlaceCard(short morph, byte perfection)
        {
            _card = new SpecialistInstance(new Item
            {
                VNum = 901, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist,
                EquipmentSlot = EquipmentType.Sp, Morph = morph,
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
