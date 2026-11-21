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
using NosCore.Algorithm.RarityPointService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using Item = NosCore.GameObject.Services.ItemGenerationService.Item.Item;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class PartnerSkillTicketHandlerTests
    {
        private TestWorld World;
        private const short PartnerSpecialist = 900;
        private const short SingleTicket = 5931;
        private const short AllTicket = 5932;
        private const byte SkillSet = 9;

        private ClientSession _session;
        private ItemGenerationService _items;
        private PartnerEquipmentService _equipment;
        private PartnerSkillTicketHandler _handler;
        private Mate _partner;
        private InventoryItemInstance _ticket;

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
                },
                new Item { Type = NoscorePocketType.Main, VNum = SingleTicket, ItemType = ItemType.Special, Effect = ItemEffectType.PartnerSkillTicket },
                new Item { Type = NoscorePocketType.Main, VNum = AllTicket, ItemType = ItemType.Special, Effect = ItemEffectType.PartnerSkillTicketAll }
            }), NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService(), new NosCore.GameObject.Services.BattleService.RandomProvider());
            World.Inject(new List<SkillDto>
            {
                new() { SkillVNum = 1315, CastId = 0, SkillType = 5, UpgradeType = SkillSet },
                new() { SkillVNum = 1316, CastId = 1, SkillType = 5, UpgradeType = SkillSet },
                new() { SkillVNum = 1317, CastId = 2, SkillType = 5, UpgradeType = SkillSet }
            });
            var specialists = World.Create<PartnerSpecialistService>();
            _equipment = new PartnerEquipmentService(specialists, World.Create<PartnerSkillCatalog>());
            _handler = new PartnerSkillTicketHandler(specialists);
        }

        [TestMethod]
        public async Task DroppingTheSingleTicketOnALearnedSkillAsksFirst()
        {
            await new Spec("Dropping the single ticket on a learned skill asks before changing anything")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheOwnerHolds_, SingleTicket)
                .And(Skill_IsRanked_, 1, SpPartnerRank.B)
                .WhenAsync(TheTicketIsDroppedOnSkill_WithMode_, 1, (short)0)
                .Then(TheOwnerShouldBeAsked_ToConfirmSkill_, Game18NConstString.AskChangeSkillLevelChosen, 1)
                .And(Skill_ShouldBeRanked_, 1, SpPartnerRank.B)
                .And(TheOwnerShouldHold_Tickets, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingTheSingleTicketUnlearnsTheSkillAndFillsTheAgility()
        {
            await new Spec("Confirming the single ticket puts that skill back to unlearned and fills the agility")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheOwnerHolds_, SingleTicket)
                .And(Skill_IsRanked_, 0, SpPartnerRank.C)
                .And(Skill_IsRanked_, 1, SpPartnerRank.B)
                .WhenAsync(TheTicketIsDroppedOnSkill_WithMode_, 1, (short)1)
                .Then(Skill_ShouldBeRanked_, 1, SpPartnerRank.NotIdentifiedYet)
                .And(Skill_ShouldBeRanked_, 0, SpPartnerRank.C)
                .And(TheCardsAgilityShouldBe_, (byte)100)
                .And(TheOwnerShouldHold_Tickets, 0)
                .And(TheOwnerShouldBeTold_, Game18NConstString.SkillLevelChanged)
                .And(ScnShouldShowSkill_AtRank_, 1, SpPartnerRank.NotIdentifiedYet)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSingleTicketRefusesARankSSkill()
        {
            await new Spec("The single ticket cannot be used on a skill of rank S")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheOwnerHolds_, SingleTicket)
                .And(Skill_IsRanked_, 2, SpPartnerRank.S)
                .WhenAsync(TheTicketIsDroppedOnSkill_WithMode_, 2, (short)1)
                .Then(Skill_ShouldBeRanked_, 2, SpPartnerRank.S)
                .And(TheOwnerShouldHold_Tickets, 1)
                .And(TheOwnerShouldBeTold_, Game18NConstString.CannotUseIfSkillLevelReached)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSingleTicketRefusesAnUnlearnedSkill()
        {
            await new Spec("The single ticket does nothing to a skill that has no rank yet")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheOwnerHolds_, SingleTicket)
                .WhenAsync(TheTicketIsDroppedOnSkill_WithMode_, 0, (short)1)
                .Then(TheCardsAgilityShouldBe_, (byte)0)
                .And(TheOwnerShouldHold_Tickets, 1)
                .And(TheOwnerShouldBeTold_, Game18NConstString.SkillLevelInsufficient)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheAllTicketWarnsAboutARankSSkill()
        {
            await new Spec("The ticket for all skills asks again when one of them is rank S")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheOwnerHolds_, AllTicket)
                .And(Skill_IsRanked_, 2, SpPartnerRank.S)
                .WhenAsync(TheTicketIsDroppedOnSkill_WithMode_, 0, (short)0)
                .Then(TheOwnerShouldBeAsked_ToConfirmSkill_, Game18NConstString.PartnersSResetItQuestion, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheAllTicketAsksToChangeEverySkill()
        {
            await new Spec("The ticket for all skills asks before changing them")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheOwnerHolds_, AllTicket)
                .And(Skill_IsRanked_, 0, SpPartnerRank.F)
                .WhenAsync(TheTicketIsDroppedOnSkill_WithMode_, 0, (short)0)
                .Then(TheOwnerShouldBeAsked_ToConfirmSkill_, Game18NConstString.WantToChangeAllSkillLevels, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingTheAllTicketUnlearnsEverySkill()
        {
            await new Spec("Confirming the ticket for all skills unlearns every skill and fills the agility")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheOwnerHolds_, AllTicket)
                .And(Skill_IsRanked_, 0, SpPartnerRank.F)
                .And(Skill_IsRanked_, 2, SpPartnerRank.S)
                .WhenAsync(TheTicketIsDroppedOnSkill_WithMode_, 1, (short)1)
                .Then(Skill_ShouldBeRanked_, 0, SpPartnerRank.NotIdentifiedYet)
                .And(Skill_ShouldBeRanked_, 2, SpPartnerRank.NotIdentifiedYet)
                .And(TheCardsAgilityShouldBe_, (byte)100)
                .And(TheOwnerShouldHold_Tickets, 0)
                .And(TheOwnerShouldBeTold_, Game18NConstString.AllSkillReset)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheAllTicketNeedsALearnedSkill()
        {
            await new Spec("The ticket for all skills needs at least one learned skill")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheOwnerHolds_, AllTicket)
                .WhenAsync(TheTicketIsDroppedOnSkill_WithMode_, 0, (short)1)
                .Then(TheOwnerShouldHold_Tickets, 1)
                .And(TheOwnerShouldBeTold_, Game18NConstString.SpecialistMustHasOneSkill)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATransformedPartnerKeepsItsSkills()
        {
            await new Spec("A transformed partner has to turn back before a ticket works")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheOwnerHolds_, SingleTicket)
                .And(Skill_IsRanked_, 0, SpPartnerRank.D)
                .And(ThePartnerIsTransformed)
                .WhenAsync(TheTicketIsDroppedOnSkill_WithMode_, 0, (short)1)
                .Then(Skill_ShouldBeRanked_, 0, SpPartnerRank.D)
                .And(TheOwnerShouldHold_Tickets, 1)
                .And(TheOwnerShouldBeTold_, Game18NConstString.CancelPartnerSpecialist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APartnerAtHomeKeepsItsSkills()
        {
            await new Spec("A ticket only works on a partner accompanying its owner")
                .GivenAsync(APartnerWearsTheCard)
                .And(TheOwnerHolds_, SingleTicket)
                .And(Skill_IsRanked_, 0, SpPartnerRank.D)
                .And(ThePartnerStaysHome)
                .WhenAsync(TheTicketIsDroppedOnSkill_WithMode_, 0, (short)1)
                .Then(Skill_ShouldBeRanked_, 0, SpPartnerRank.D)
                .And(TheOwnerShouldBeTold_, Game18NConstString.PartnerMustAccompanying)
                .ExecuteAsync();
        }

        private async Task APartnerWearsTheCard()
        {
            _partner = TestMates.Partner(_session.Character.CharacterId);
            _session.Character.Mates[_partner.MateTransportId] = _partner;
            await _partner.SpawnAsync(_session.Character, _session.Character.MapInstance, 5, 5);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(PartnerSpecialist, 1), _session.Character.CharacterId),
                NoscorePocketType.Equipment, 0);
            await _equipment.WearAsync(_session.Character, 0, 1);
        }

        private void TheOwnerHolds_(short vnum)
        {
            _ticket = _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(vnum, 1), _session.Character.CharacterId),
                NoscorePocketType.Main, 0).NotNull().Single();
        }

        private SpecialistInstance Card() => _partner.Specialist(_session.Character.InventoryService).NotNull();

        private void Skill_IsRanked_(int slot, SpPartnerRank rank) => Card().SetPartnerSkillRank(slot, rank);

        private void ThePartnerIsTransformed() => _partner.SpecialistMorph = Card().Item.Morph;

        private void ThePartnerStaysHome() => _partner.IsTeamMember = false;

        private Task TheTicketIsDroppedOnSkill_WithMode_(int skillSlot, short mode)
        {
            _session.LastPackets.Clear();
            return _handler.Handle(new ItemUsedEvent(_session, _ticket, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = _ticket.Slot,
                Mode = mode,
                Parameter = 1,
                TargetType = (PocketType)_partner.PetSlot,
                TargetSlot = (short)skillSlot
            }));
        }

        private void TheOwnerShouldBeAsked_ToConfirmSkill_(Game18NConstString question, int skillSlot)
        {
            var qna = _session.LastPackets.OfType<QnaiPacket>().Single();
            Assert.AreEqual(question, qna.Question);
            PacketAssert.AreEqual(new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = _ticket.Slot,
                Mode = 1,
                Parameter = 1,
                TargetType = (PocketType)_partner.PetSlot,
                TargetSlot = (short)skillSlot
            }, (UseItemPacket)qna.YesPacket.NotNull());
        }

        private void Skill_ShouldBeRanked_(int slot, SpPartnerRank rank) =>
            Assert.AreEqual(rank, Card().PartnerSkillRank(slot));

        private void TheCardsAgilityShouldBe_(byte agility) => Assert.AreEqual(agility, Card().Agility);

        private void TheOwnerShouldHold_Tickets(int amount) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.Values
                .Where(s => s.ItemInstance.ItemVNum is SingleTicket or AllTicket).Sum(s => s.ItemInstance.Amount));

        private void TheOwnerShouldBeTold_(Game18NConstString message) =>
            PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = message },
                _session.LastPackets.OfType<MsgiPacket>().Single());

        private void ScnShouldShowSkill_AtRank_(int slot, SpPartnerRank rank)
        {
            var scn = _session.LastPackets.OfType<ScnPacket>().Last();
            var skill = slot switch { 0 => scn.Skill1Details, 1 => scn.Skill2Details, _ => scn.Skill3Details };
            PacketAssert.AreEqual(new ScnPacket.ScSkillDetails { SkillId = (short)(1315 + slot), Rank = rank }, skill.NotNull());
        }
    }
}
