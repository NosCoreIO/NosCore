//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SkillReagentTests
    {
        private const short Bandage = 2087;

        private TestWorld World;
        private ClientSession _session;
        private CharacterSkill _skill;
        private SkillCastService _service;
        private SkillCastRefusal _refusal;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.ItemList.Add(new Item { Type = NoscorePocketType.Etc, VNum = Bandage });
            _session = await World.CreateSessionAsync();
            _session.Character.Mp = 100;
            World.Freeze<Mock<IEquipmentStatsService>>()
                .Setup(g => g.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            _service = World.Create<SkillCastService>();
        }

        [TestMethod]
        public async Task ASkillIsRefusedWithoutItsReagent()
        {
            await new Spec("A skill naming a reagent item is refused to a character who carries none")
                .Given(TheCharacterKnowsASkillNeeding_, Bandage)
                .When(TheCastIsChecked)
                .Then(TheCastShouldBe_, SkillCastRefusal.MissingItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillIsAcceptedWithItsReagent()
        {
            await new Spec("A skill naming a reagent item is accepted from a character who carries one")
                .Given(TheCharacterKnowsASkillNeeding_, Bandage)
                .And(TheCharacterCarries_Of_, (short)1, Bandage)
                .When(TheCastIsChecked)
                .Then(TheCastShouldBe_, SkillCastRefusal.None)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NotEnoughManaIsReportedBeforeTheReagent()
        {
            await new Spec("A character short of both mana and the reagent is told about the mana")
                .Given(TheCharacterKnowsASkillNeeding_, Bandage)
                .And(TheSkillCosts_Mp, (short)500)
                .When(TheCastIsChecked)
                .Then(TheCastShouldBe_, SkillCastRefusal.NotEnoughMp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAcceptedCastUsesUpOneReagent()
        {
            await new Spec("An accepted cast takes one reagent and shows the caster the stack it leaves")
                .Given(TheCharacterKnowsASkillNeeding_, Bandage)
                .And(TheCharacterCarries_Of_, (short)3, Bandage)
                .WhenAsync(TheCastIsSpent)
                .Then(TheCharacterShouldCarry_Of_, 2, Bandage)
                .And(TheBagShouldShowSlot0Holding_, (short)2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheLastReagentEmptiesItsSlot()
        {
            await new Spec("A cast that uses the last reagent empties its slot, and the next cast is refused")
                .Given(TheCharacterKnowsASkillNeeding_, Bandage)
                .And(TheCharacterCarries_Of_, (short)1, Bandage)
                .WhenAsync(TheCastIsSpent)
                .And(TheCooldownIsOver)
                .And(TheCastIsChecked)
                .Then(TheCharacterShouldCarry_Of_, 0, Bandage)
                .And(TheBagShouldShowSlot0Empty)
                .And(TheCastShouldBe_, SkillCastRefusal.MissingItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASkillWithoutAReagentLeavesTheBagAlone()
        {
            await new Spec("A skill that names no item is accepted and takes nothing from the bag")
                .Given(TheCharacterKnowsASkillNeeding_, (short)0)
                .And(TheCharacterCarries_Of_, (short)1, Bandage)
                .WhenAsync(TheCastIsSpent)
                .And(TheCooldownIsOver)
                .And(TheCastIsChecked)
                .Then(TheCastShouldBe_, SkillCastRefusal.None)
                .And(TheCharacterShouldCarry_Of_, 1, Bandage)
                .And(NoBagChangeShouldBeSent)
                .ExecuteAsync();
        }

        private void TheCharacterKnowsASkillNeeding_(short itemVNum) =>
            _skill = new CharacterSkill
            {
                CharacterId = _session.Character.CharacterId,
                SkillVNum = 238,
                Skill = new SkillDto { SkillVNum = 238, CastId = 16, MpCost = 15, Cooldown = 50, ItemVNum = itemVNum },
            };

        private void TheSkillCosts_Mp(short mpCost) => _skill.Skill.NotNull().MpCost = mpCost;

        private void TheCharacterCarries_Of_(short amount, short vnum) =>
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                World.GenerateItemProvider().Create(vnum, amount), _session.Character.CharacterId), NoscorePocketType.Etc, 0);

        private void TheCastIsChecked() => _refusal = _service.Check(_session.Character, _skill, out _);

        private async Task TheCastIsSpent()
        {
            _service.Check(_session.Character, _skill, out var mpCost);
            await _service.SpendAsync(_session.Character, _skill, mpCost);
        }

        private void TheCooldownIsOver() => World.Clock.Advance(Duration.FromSeconds(6));

        private void TheCastShouldBe_(SkillCastRefusal refusal) => Assert.AreEqual(refusal, _refusal);

        private void TheCharacterShouldCarry_Of_(int amount, short vnum) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(vnum));

        private void TheBagShouldShowSlot0Holding_(short amount) =>
            PacketAssert.AreEqual(new IvnPacket
            {
                Type = PocketType.Etc,
                IvnSubPackets =
                [
                    new IvnSubPacket
                    {
                        Slot = 0, VNum = Bandage, RareAmount = amount, UpgradeDesign = 0, SecondUpgrade = null, Unknown5 = null, Unknown6 = null,
                    },
                ],
            }, _session.LastPackets.OfType<IvnPacket>().Single());

        private void TheBagShouldShowSlot0Empty()
        {
            var change = _session.LastPackets.OfType<IvnPacket>().Single();
            Assert.AreEqual(PocketType.Etc, change.Type);
            var slot = change.IvnSubPackets.NotNull().Single();
            Assert.AreEqual((short)0, slot.Slot);
            Assert.AreNotEqual(Bandage, slot.VNum);
        }

        private void NoBagChangeShouldBeSent() =>
            Assert.IsFalse(_session.LastPackets.OfType<IvnPacket>().Any());
    }
}
