//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Algorithm.PartnerSkillRankService;
using NosCore.Algorithm.RarityPointService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.PacketHandlers.Mates;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using Item = NosCore.GameObject.Services.ItemGenerationService.Item.Item;

namespace NosCore.PacketHandlers.Tests.Mates
{
    [TestClass]
    public class PsopPacketHandlerTests
    {
        private TestWorld World;
        private const short PartnerSpecialist = 900;
        private const byte SkillSet = 9;

        private ClientSession _session;
        private ItemGenerationService _items;
        private PartnerSpecialistService _specialists;
        private PartnerEquipmentService _equipment;
        private PsopPacketHandler _handler;
        private Mock<NosCore.GameObject.Services.BattleService.IRandomProvider> _random;
        private Mate _partner;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(0);
            _items = new ItemGenerationService(new ItemCatalog(new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = PartnerSpecialist, ItemType = ItemType.Specialist,
                    EquipmentSlot = EquipmentType.Sp, ItemSubType = 4, Morph = 2041, PartnerSkillSet = SkillSet
                }
            }), NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService(), new NosCore.GameObject.Services.BattleService.RandomProvider());
            World.Inject(new List<SkillDto>
            {
                new() { SkillVNum = 1315, CastId = 0, SkillType = 5, UpgradeType = SkillSet },
                new() { SkillVNum = 1316, CastId = 1, SkillType = 5, UpgradeType = SkillSet },
                new() { SkillVNum = 1317, CastId = 2, SkillType = 5, UpgradeType = SkillSet }
            });
            World.Inject<IPartnerSkillRankService>(new PartnerSkillRankService());
            _random = World.Random;
            _specialists = World.Create<PartnerSpecialistService>();
            _equipment = new PartnerEquipmentService(_specialists, World.Create<PartnerSkillCatalog>());
            _handler = new PsopPacketHandler(_specialists);
        }

        [TestMethod]
        public async Task APartnerWithFullAgilityLearnsTheSkill()
        {
            await new Spec("Clicking a skill of a card at full agility gives it a rank and empties the agility")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheCardsAgilityIs_, (byte)100)
                .And(TheRollIs_, 0.5)
                .WhenAsync(TheOwnerLearnsSkill_, (byte)1)
                .Then(Skill_ShouldBeRanked_, 1, SpPartnerRank.D)
                .And(TheCardsAgilityShouldBe_, (byte)0)
                .And(TheOwnerShouldBeTold_, Game18NConstString.YourPartnerNowHasSkill)
                .And(ScnShouldShowSkill_AtRank_, 1, SpPartnerRank.D)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheHighestRollGivesRankS()
        {
            await new Spec("The top of the roll gives rank S")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheCardsAgilityIs_, (byte)100)
                .And(TheRollIs_, 0.999)
                .WhenAsync(TheOwnerLearnsSkill_, (byte)0)
                .Then(Skill_ShouldBeRanked_, 0, SpPartnerRank.S)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACardWithoutFullAgilityCannotLearn()
        {
            await new Spec("A card short of full agility learns nothing")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheCardsAgilityIs_, (byte)99)
                .WhenAsync(TheOwnerLearnsSkill_, (byte)0)
                .Then(Skill_ShouldBeRanked_, 0, SpPartnerRank.NotIdentifiedYet)
                .And(TheCardsAgilityShouldBe_, (byte)99)
                .And(TheOwnerShouldBeTold_, Game18NConstString.PartnerNeedAgility)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ALearnedSkillIsNotLearnedAgain()
        {
            await new Spec("A skill that already has a rank keeps it and the agility stays full")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheCardsAgilityIs_, (byte)100)
                .And(Skill_IsRanked_, 2, SpPartnerRank.B)
                .WhenAsync(TheOwnerLearnsSkill_, (byte)2)
                .Then(Skill_ShouldBeRanked_, 2, SpPartnerRank.B)
                .And(TheCardsAgilityShouldBe_, (byte)100)
                .And(TheOwnerShouldBeTold_, Game18NConstString.PartnerHasSkill)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATransformedPartnerCannotLearn()
        {
            await new Spec("A transformed partner has to turn back before it learns a skill")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheCardsAgilityIs_, (byte)100)
                .And(ThePartnerIsTransformed)
                .WhenAsync(TheOwnerLearnsSkill_, (byte)0)
                .Then(Skill_ShouldBeRanked_, 0, SpPartnerRank.NotIdentifiedYet)
                .And(TheOwnerShouldBeTold_, Game18NConstString.CancelPartnerSpecialist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APartnerWithoutACardCannotLearn()
        {
            await new Spec("A partner without a specialist card is told to wear one")
                .GivenAsync(APartnerIsOut)
                .WhenAsync(TheOwnerLearnsSkill_, (byte)0)
                .Then(TheOwnerShouldBeTold_, Game18NConstString.EquipPartnerWithSpecialist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APartnerAtHomeCannotLearn()
        {
            await new Spec("A partner that is not accompanying the owner cannot learn")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheCardsAgilityIs_, (byte)100)
                .And(ThePartnerStaysHome)
                .WhenAsync(TheOwnerLearnsSkill_, (byte)0)
                .Then(Skill_ShouldBeRanked_, 0, SpPartnerRank.NotIdentifiedYet)
                .And(TheOwnerShouldBeTold_, Game18NConstString.PartnerMustAccompanying)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheWindowShowsTheCardsSkillsWithoutTransforming()
        {
            await new Spec("sc_n lists the card's skills with their ranks and the agility while the partner is not transformed")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheCardsAgilityIs_, (byte)40)
                .And(Skill_IsRanked_, 0, SpPartnerRank.A)
                .WhenAsync(TheOwnerTakesTheCardOffAndPutsItBack)
                .Then(ScnShouldShowSkill_AtRank_, 0, SpPartnerRank.A)
                .And(ScnShouldShowSkill_AtRank_, 1, SpPartnerRank.NotIdentifiedYet)
                .And(ScnShouldShowAgility_, (byte)40)
                .ExecuteAsync();
        }

        private async Task APartnerIsOut()
        {
            _partner = TestMates.Partner(_session.Character.CharacterId);
            _session.Character.Mates[_partner.MateTransportId] = _partner;
            await _partner.SpawnAsync(_session.Character, _session.Character.MapInstance, 5, 5);
        }

        private async Task APartnerWearsTheCard()
        {
            await APartnerIsOut();
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(PartnerSpecialist, 1), _session.Character.CharacterId),
                NoscorePocketType.Equipment, 0);
            await _equipment.WearAsync(_session.Character, 0, 1);
        }

        private async Task TheOwnerTakesTheCardOffAndPutsItBack()
        {
            await _equipment.RemoveAsync(_session.Character, EquipmentType.Sp, 1);
            var slot = _session.Character.InventoryService.Values
                .First(s => s.ItemInstance.ItemVNum == PartnerSpecialist).Slot;
            _session.LastPackets.Clear();
            await _equipment.WearAsync(_session.Character, (byte)slot, 1);
        }

        private SpecialistInstance Card() => _partner.Specialist(_session.Character.InventoryService).NotNull();

        private void TheCardsAgilityIs_(byte agility) => Card().Agility = agility;

        private void Skill_IsRanked_(int slot, SpPartnerRank rank) => Card().SetPartnerSkillRank(slot, rank);

        private void TheRollIs_(double roll) => _random.Setup(s => s.NextDouble()).Returns(roll);

        private void ThePartnerIsTransformed() => _partner.SpecialistMorph = Card().Item.Morph;

        private void ThePartnerStaysHome() => _partner.IsTeamMember = false;

        private Task TheOwnerLearnsSkill_(byte slot)
        {
            _session.LastPackets.Clear();
            return _handler.ExecuteAsync(new PsopPacket { PetSlot = _partner.PetSlot, SkillSlot = slot }, _session);
        }

        private void Skill_ShouldBeRanked_(int slot, SpPartnerRank rank) =>
            Assert.AreEqual(rank, Card().PartnerSkillRank(slot));

        private void TheCardsAgilityShouldBe_(byte agility) => Assert.AreEqual(agility, Card().Agility);

        private void TheOwnerShouldBeTold_(Game18NConstString message) =>
            PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = message },
                _session.LastPackets.OfType<MsgiPacket>().Single());

        private void ScnShouldShowSkill_AtRank_(int slot, SpPartnerRank rank)
        {
            var scn = _session.LastPackets.OfType<ScnPacket>().Last();
            var skill = slot switch { 0 => scn.Skill1Details, 1 => scn.Skill2Details, _ => scn.Skill3Details };
            PacketAssert.AreEqual(new ScnPacket.ScSkillDetails { SkillId = (short)(1315 + slot), Rank = rank }, skill.NotNull());
        }

        private void ScnShouldShowAgility_(byte agility) =>
            Assert.AreEqual(agility, _session.LastPackets.OfType<ScnPacket>().Last().SpDetails.NotNull().AgilityPercentage);
    }
}
