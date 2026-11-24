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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SkillService;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.UpgradeService
{
    [TestClass]
    public class TattooApplicationOperationTests : SpecBase
    {
        private const short BearPatternVNum = 5790;
        private const byte PatternSlot = 2;
        private const byte BearLoa = 1;
        private const byte LionLoa = 2;
        private const long Gold = 20_000;
        private const short GolemCore = 2406;
        private const short GolemCores = 10;
        private const short LoaRunicPowder = 2411;
        private const short LoaRunicPowders = 15;

        private static readonly SkillDto Cower = Tattoo(686, BearLoa);
        private static readonly SkillDto BearStamina = Tattoo(688, BearLoa);
        private static readonly SkillDto LionClaws = Tattoo(695, LionLoa);
        private static readonly SkillDto EagleEyes = Tattoo(704, 3);
        private static readonly SkillDto SnakeVenom = Tattoo(709, 4);

        private TattooApplicationOperation _operation;
        private IReadOnlyList<IPacket> _result = Array.Empty<IPacket>();

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            World.Inject(new List<SkillDto> { Cower, BearStamina, LionClaws, EagleEyes, SnakeVenom });
            World.Inject(new List<UpgradeCostDto> { new() { UpgradeCostId = 1, Kind = UpgradePacketType.ApplyTattoo, Gold = Gold } });
            World.Inject(new List<UpgradeCostMaterialDto>
            {
                new() { UpgradeCostMaterialId = 1, UpgradeCostId = 1, ItemVNum = GolemCore, Amount = GolemCores },
                new() { UpgradeCostMaterialId = 2, UpgradeCostId = 1, ItemVNum = LoaRunicPowder, Amount = LoaRunicPowders },
            });
            World.UseReal<ISkillService, NosCore.GameObject.Services.SkillService.SkillService>();
            World.Freeze<Mock<IRandomProvider>>().Setup(r => r.Next(0, 2)).Returns(1);
            _operation = World.Create<TattooApplicationOperation>();
        }

        [TestMethod]
        public async Task APatternInksOneOfItsLoasTattoos()
        {
            await new Spec("A bear pattern inks one of the bear tattoos, which is stored and listed, and the pattern, gold and materials are used up")
                .Given(BearPatterns_, (short)2)
                .And(TheCharacterHas_Gold_Cores_Powders, Gold + 5, (short)(GolemCores + 1), LoaRunicPowders)
                .WhenAsync(ApplyingThePattern)
                .Then(TheCharacterShouldKnow_, BearStamina.SkillVNum)
                .AndAsync(TheTattooShouldBeStored_, BearStamina.SkillVNum)
                .And(TheSkillListShouldInclude_, BearStamina.SkillVNum)
                .And(TheAppliedMessageShouldName_, BearStamina.SkillVNum)
                .And(PatternsLeftShouldBe_, 1)
                .And(GoldLeftShouldBe_, 5L)
                .And(CoresAndPowdersLeftShouldBe_, 1, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATattooNeedsTheGold()
        {
            await new Spec("Without the gold nothing is inked and nothing is spent")
                .Given(BearPatterns_, (short)1)
                .And(TheCharacterHas_Gold_Cores_Powders, Gold - 1, GolemCores, LoaRunicPowders)
                .WhenAsync(ApplyingThePattern)
                .Then(TheInfoShouldBe_, Game18NConstString.NotEnoughGold)
                .And(TheCharacterShouldNotKnow_, BearStamina.SkillVNum)
                .And(PatternsLeftShouldBe_, 1)
                .And(GoldLeftShouldBe_, Gold - 1)
                .And(CoresAndPowdersLeftShouldBe_, (int)GolemCores, (int)LoaRunicPowders)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATattooNeedsEveryMaterial()
        {
            await new Spec("With one material short nothing is inked and nothing is spent")
                .Given(BearPatterns_, (short)1)
                .And(TheCharacterHas_Gold_Cores_Powders, Gold, GolemCores, (short)(LoaRunicPowders - 1))
                .WhenAsync(ApplyingThePattern)
                .Then(TheInfoShouldBe_, Game18NConstString.NotEnoughIngredients)
                .And(TheCharacterShouldNotKnow_, BearStamina.SkillVNum)
                .And(PatternsLeftShouldBe_, 1)
                .And(GoldLeftShouldBe_, Gold)
                .And(CoresAndPowdersLeftShouldBe_, (int)GolemCores, LoaRunicPowders - 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASecondTattooOfTheSameLoaIsRefused()
        {
            await new Spec("A character with a bear tattoo cannot be inked with another, and keeps the pattern")
                .Given(BearPatterns_, (short)1)
                .And(TheCharacterWears_, Cower)
                .WhenAsync(ApplyingThePattern)
                .Then(TheMessageShouldBe_, Game18NConstString.AlreadyHaveTatooType)
                .And(TheCharacterShouldNotKnow_, BearStamina.SkillVNum)
                .And(PatternsLeftShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AThirdTattooIsRefused()
        {
            await new Spec("A character wearing two tattoos cannot get a third")
                .Given(BearPatterns_, (short)1)
                .And(TheCharacterWears_, EagleEyes)
                .And(TheCharacterWears_, SnakeVenom)
                .WhenAsync(ApplyingThePattern)
                .Then(TheMessageShouldBe_, Game18NConstString.CanNotHaveOtherTatoo)
                .And(TheCharacterShouldNotKnow_, BearStamina.SkillVNum)
                .And(PatternsLeftShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATattooOfAnotherLoaCanBeAdded()
        {
            await new Spec("A character with a lion tattoo can still be inked with a bear one")
                .Given(BearPatterns_, (short)1)
                .And(TheCharacterHas_Gold_Cores_Powders, Gold, GolemCores, LoaRunicPowders)
                .And(TheCharacterWears_, LionClaws)
                .WhenAsync(ApplyingThePattern)
                .Then(TheCharacterShouldKnow_, BearStamina.SkillVNum)
                .And(TheCharacterShouldKnow_, LionClaws.SkillVNum)
                .And(PatternsLeftShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnItemThatIsNotAPatternIsIgnored()
        {
            await new Spec("Another item in the slot inks nothing and is kept")
                .Given(AnotherItemInThePatternSlot)
                .WhenAsync(ApplyingThePattern)
                .Then(NothingShouldBeSent)
                .And(TheCharacterShouldNotKnow_, BearStamina.SkillVNum)
                .ExecuteAsync();
        }

        private static SkillDto Tattoo(short vnum, byte loa) =>
            new() { SkillVNum = vnum, SkillType = 1, Class = 27, CastId = (short)(39 + loa) };

        private void BearPatterns_(short amount) =>
            PlaceInPatternSlot(new Item { VNum = BearPatternVNum, Type = NoscorePocketType.Main, ItemType = ItemType.Special,
                Effect = ItemEffectType.TattooPattern, EffectValue = BearLoa }, amount);

        private void AnotherItemInThePatternSlot() =>
            PlaceInPatternSlot(new Item { VNum = 1012, Type = NoscorePocketType.Main }, 1);

        private void TheCharacterHas_Gold_Cores_Powders(long gold, short cores, short powders)
        {
            Session.Character.Gold = gold;
            Place(new Item { VNum = GolemCore, Type = NoscorePocketType.Main }, cores, PatternSlot + 1);
            Place(new Item { VNum = LoaRunicPowder, Type = NoscorePocketType.Main }, powders, PatternSlot + 2);
        }

        private void PlaceInPatternSlot(Item item, short amount) => Place(item, amount, PatternSlot);

        private void Place(Item item, short amount, int slot)
        {
            var inst = InventoryItemInstance.Create(new ItemInstance(item) { Amount = amount }, Session.Character.CharacterId);
            inst.Slot = (short)slot;
            inst.Type = NoscorePocketType.Main;
            Session.Character.InventoryService[inst.ItemInstanceId] = inst;
        }

        private void TheCharacterWears_(SkillDto tattoo) =>
            Session.Character.Skills[tattoo.SkillVNum] = new CharacterSkill { SkillVNum = tattoo.SkillVNum, Skill = tattoo };

        private async Task ApplyingThePattern() =>
            _result = await _operation.ExecuteAsync(Session, new UpgradePacket
            {
                UpgradeType = UpgradePacketType.ApplyTattoo,
                InventoryType = PocketType.Main,
                Slot = PatternSlot,
            });

        private void TheCharacterShouldKnow_(short vnum) => Assert.IsTrue(Session.Character.Skills.ContainsKey(vnum));

        private void TheCharacterShouldNotKnow_(short vnum) => Assert.IsFalse(Session.Character.Skills.ContainsKey(vnum));

        private async Task TheTattooShouldBeStored_(short vnum)
        {
            var characterId = Session.Character.CharacterId;
            Assert.IsNotNull(await World.CharacterSkillDao.FirstOrDefaultAsync(s => s.CharacterId == characterId && s.SkillVNum == vnum));
        }

        private void TheSkillListShouldInclude_(short vnum) =>
            CollectionAssert.Contains(_result.OfType<SkiPacket>().Single().SkillVnums, vnum);

        private void TheAppliedMessageShouldName_(short vnum)
        {
            var message = _result.OfType<MsgiPacket>().Single();
            Assert.AreEqual(Game18NConstString.TattooAplied, message.Message);
            Assert.AreEqual((byte)5, message.ArgumentType);
            Assert.AreEqual(vnum, message.Game18NArguments[0]);
        }

        private void TheMessageShouldBe_(Game18NConstString message) =>
            Assert.AreEqual(message, _result.OfType<MsgiPacket>().Single().Message);

        private void PatternsLeftShouldBe_(int amount) =>
            Assert.AreEqual(amount, Session.Character.InventoryService.CountItem(BearPatternVNum));

        private void TheInfoShouldBe_(Game18NConstString message) =>
            Assert.AreEqual(message, _result.OfType<InfoiPacket>().Single().Message);

        private void GoldLeftShouldBe_(long gold) => Assert.AreEqual(gold, Session.Character.Gold);

        private void CoresAndPowdersLeftShouldBe_(int cores, int powders)
        {
            Assert.AreEqual(cores, Session.Character.InventoryService.CountItem(GolemCore));
            Assert.AreEqual(powders, Session.Character.InventoryService.CountItem(LoaRunicPowder));
        }

        private void NothingShouldBeSent() => Assert.AreEqual(0, _result.Count);
    }
}
