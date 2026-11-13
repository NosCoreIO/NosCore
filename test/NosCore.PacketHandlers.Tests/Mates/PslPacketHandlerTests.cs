//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MateService;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Mates;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Tests.Shared;
using SpecLight;
using Item = NosCore.GameObject.Services.ItemGenerationService.Item.Item;

namespace NosCore.PacketHandlers.Tests.Mates
{
    [TestClass]
    public class PslPacketHandlerTests
    {
        private TestWorld World;
        private const short PartnerSpecialist = 900;
        private const short PlayerSpecialist = 901;
        private const short SpecialistMorph = 2041;
        private const byte SkillSet = 9;

        private ClientSession _session;
        private ItemGenerationService _items;
        private PartnerSpecialistService _specialists;
        private PartnerEquipmentService _equipment;
        private PslPacketHandler _handler;
        private Mate _partner;
        private Mock<ISessionGroup> _onlookers;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(0);
            _onlookers = new Mock<ISessionGroup>();
            _session.Character.MapInstance.Sessions = _onlookers.Object;
            _items = new ItemGenerationService(new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = PartnerSpecialist, ItemType = ItemType.Specialist,
                    EquipmentSlot = EquipmentType.Sp, ItemSubType = 4, Morph = SpecialistMorph,
                    PartnerSkillSet = SkillSet
                },
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = PlayerSpecialist, ItemType = ItemType.Specialist,
                    EquipmentSlot = EquipmentType.Sp, ItemSubType = 0, Morph = 1
                }
            }, NullLoggerFactory.Instance, World.LogLanguageLocalizer, new RarityPointService());
            World.Inject(new List<SkillDto>
            {
                new() { SkillVNum = 1317, CastId = 2, SkillType = 5, UpgradeType = SkillSet },
                new() { SkillVNum = 1315, CastId = 0, SkillType = 5, UpgradeType = SkillSet },
                new() { SkillVNum = 1316, CastId = 1, SkillType = 5, UpgradeType = SkillSet },
                new() { SkillVNum = 1318, CastId = 0, SkillType = 5, UpgradeType = SkillSet + 1 },
                new() { SkillVNum = 1000, CastId = 0, SkillType = 1, UpgradeType = SkillSet }
            });
            _specialists = World.Create<PartnerSpecialistService>();
            _equipment = new PartnerEquipmentService(_specialists);
            _handler = new PslPacketHandler(_specialists);
        }

        [TestMethod]
        public async Task APartnerSpecialistCardCanBeWornByAPartner()
        {
            await new Spec("A partner specialist card dropped on a partner goes into its specialist slot and shows in sc_n")
                .GivenAsync(APartnerIsOut)
                .WhenAsync(TheOwnerDrops_OnThePartner, PartnerSpecialist)
                .Then(ThePartnerShouldWear_, PartnerSpecialist)
                .And(ScnShouldListTheSpecialist_, PartnerSpecialist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APlayersSpecialistCardIsRefused()
        {
            await new Spec("A player's specialist card is refused on a partner")
                .GivenAsync(APartnerIsOut)
                .WhenAsync(TheOwnerDrops_OnThePartner, PlayerSpecialist)
                .Then(ThePartnerShouldWearNoSpecialist)
                .And(TheOwnerShouldBeTold_, Game18NConstString.PartnerCanNotUseThatEquipment)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ThePartnerButtonStartsTheTransformationBar()
        {
            await new Spec("psl 0 starts a five-second bar that ends in psl 1, and the partner dances")
                .GivenAsync(APartnerIsOut)
                .AndAsync(TheOwnerDrops_OnThePartner, PartnerSpecialist)
                .WhenAsync(TheOwnerSendsPsl_, PslPacketType.Unwear)
                .Then(AFiveSecondBarEndingInPsl1ShouldStart)
                .And(ThePartnerShouldDance)
                .And(ThePartnerShouldNotBeTransformed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ThePartnerTransformsWhenTheBarRunsOut()
        {
            await new Spec("Once the bar has run, psl 1 transforms the partner into its specialist")
                .GivenAsync(APartnerIsOut)
                .AndAsync(TheOwnerDrops_OnThePartner, PartnerSpecialist)
                .AndAsync(TheOwnerSendsPsl_, PslPacketType.Unwear)
                .And(_MillisecondsPass, 5000)
                .WhenAsync(TheOwnerSendsPsl_, PslPacketType.Wear)
                .Then(ThePartnerShouldLookLike_, SpecialistMorph)
                .And(TheTransformationEffectShouldPlay)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATransformedPartnerGetsItsCardsSkillBar()
        {
            await new Spec("Transforming fills the partner skill bar with its card's three skills in slot order")
                .GivenAsync(APartnerIsOut)
                .AndAsync(TheOwnerDrops_OnThePartner, PartnerSpecialist)
                .AndAsync(TheOwnerSendsPsl_, PslPacketType.Unwear)
                .And(_MillisecondsPass, 5000)
                .WhenAsync(TheOwnerSendsPsl_, PslPacketType.Wear)
                .Then(TheSkillBarShouldHold_, new short[] { 1315, 1316, 1317 })
                .And(TheOnlookersShouldSeeTheSkills_, new short[] { 1315, 1316, 1317 })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TurningBackClearsTheSkillBar()
        {
            await new Spec("Turning a transformed partner back takes its skill bar away")
                .GivenAsync(APartnerIsOut)
                .AndAsync(TheOwnerDrops_OnThePartner, PartnerSpecialist)
                .AndAsync(ThePartnerIsTransformed)
                .WhenAsync(TheOwnerSendsPsl_, PslPacketType.Unwear)
                .Then(TheSkillBarShouldBeCleared)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task Psl1WithoutAFinishedBarIsIgnored()
        {
            await new Spec("psl 1 sent before the bar has run its length does not transform the partner")
                .GivenAsync(APartnerIsOut)
                .AndAsync(TheOwnerDrops_OnThePartner, PartnerSpecialist)
                .AndAsync(TheOwnerSendsPsl_, PslPacketType.Unwear)
                .And(_MillisecondsPass, 1000)
                .WhenAsync(TheOwnerSendsPsl_, PslPacketType.Wear)
                .Then(ThePartnerShouldNotBeTransformed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ThePartnerButtonTurnsATransformedPartnerBack()
        {
            await new Spec("psl 0 on a transformed partner turns it back and starts a 30 second cooldown")
                .GivenAsync(APartnerIsOut)
                .AndAsync(TheOwnerDrops_OnThePartner, PartnerSpecialist)
                .AndAsync(ThePartnerIsTransformed)
                .WhenAsync(TheOwnerSendsPsl_, PslPacketType.Unwear)
                .Then(ThePartnerShouldLookLike_, (short)-1)
                .And(TheCooldownShouldBeShownFor_Seconds, 30)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ThePartnerCannotTransformDuringTheCooldown()
        {
            await new Spec("Within the cooldown psl 0 shows the seconds left and starts no bar")
                .GivenAsync(APartnerIsOut)
                .AndAsync(TheOwnerDrops_OnThePartner, PartnerSpecialist)
                .AndAsync(ThePartnerIsTransformed)
                .AndAsync(TheOwnerSendsPsl_, PslPacketType.Unwear)
                .And(_MillisecondsPass, 10000)
                .WhenAsync(TheOwnerSendsPsl_, PslPacketType.Unwear)
                .Then(TheOwnerShouldBeTold_, Game18NConstString.CantTrasformWithSideEffect)
                .And(NoBarShouldStart)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ThePartnerCanTransformAgainAfterTheCooldown()
        {
            await new Spec("Once the 30 seconds have passed psl 0 starts the bar again")
                .GivenAsync(APartnerIsOut)
                .AndAsync(TheOwnerDrops_OnThePartner, PartnerSpecialist)
                .AndAsync(ThePartnerIsTransformed)
                .AndAsync(TheOwnerSendsPsl_, PslPacketType.Unwear)
                .And(_MillisecondsPass, 30000)
                .WhenAsync(TheOwnerSendsPsl_, PslPacketType.Unwear)
                .Then(AFiveSecondBarEndingInPsl1ShouldStart)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APartnerWithoutASpecialistCardCannotTransform()
        {
            await new Spec("psl 0 for a partner without a specialist card says none is equipped")
                .GivenAsync(APartnerIsOut)
                .WhenAsync(TheOwnerSendsPsl_, PslPacketType.Unwear)
                .Then(TheOwnerShouldBeTold_, Game18NConstString.NoSpecialistCardEquipped)
                .And(NoBarShouldStart)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TakingTheCardOffTurnsThePartnerBack()
        {
            await new Spec("Taking the card off a transformed partner turns it back")
                .GivenAsync(APartnerIsOut)
                .AndAsync(TheOwnerDrops_OnThePartner, PartnerSpecialist)
                .AndAsync(ThePartnerIsTransformed)
                .WhenAsync(TheOwnerTakesTheCardOff)
                .Then(ThePartnerShouldLookLike_, (short)-1)
                .And(ThePartnerShouldWearNoSpecialist)
                .ExecuteAsync();
        }

        private async Task APartnerIsOut()
        {
            _partner = TestMates.Partner(_session.Character.CharacterId);
            _session.Character.Mates[_partner.MateTransportId] = _partner;
            await _partner.SpawnAsync(_session.Character, _session.Character.MapInstance, 5, 5);
        }

        private Task TheOwnerDrops_OnThePartner(short vnum)
        {
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(vnum, 1), _session.Character.CharacterId),
                NoscorePocketType.Equipment, 0);
            return _equipment.WearAsync(_session.Character, 0, 1);
        }

        private Task TheOwnerTakesTheCardOff() => _equipment.RemoveAsync(_session.Character, EquipmentType.Sp, 1);

        private async Task ThePartnerIsTransformed()
        {
            await TheOwnerSendsPsl_(PslPacketType.Unwear);
            _MillisecondsPass(5000);
            await TheOwnerSendsPsl_(PslPacketType.Wear);
            _session.LastPackets.Clear();
            _onlookers.Invocations.Clear();
        }

        private Task TheOwnerSendsPsl_(PslPacketType type) =>
            _handler.ExecuteAsync(new PslPacket { Type = type }, _session);

        private IEnumerable<IPacket> Broadcast() =>
            _onlookers.Invocations.SelectMany(i => i.Arguments)
                .SelectMany(a => a is IEnumerable<IPacket> many ? many : a is IPacket one ? new[] { one } : []);

        private void _MillisecondsPass(int milliseconds) =>
            World.Clock.Advance(Duration.FromMilliseconds(milliseconds));

        private void ThePartnerShouldWear_(short vnum) =>
            Assert.AreEqual(vnum, _partner.Specialist(_session.Character.InventoryService)?.ItemVNum);

        private void ThePartnerShouldWearNoSpecialist() =>
            Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType((short)EquipmentType.Sp,
                NoscorePocketType.FirstPartnerInventory));

        private void ScnShouldListTheSpecialist_(short vnum) =>
            Assert.AreEqual(vnum, _session.LastPackets.OfType<ScnPacket>().Last().SpDetails?.ItemId);

        private void TheOwnerShouldBeTold_(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(s => s.Message == message)
                || _session.LastPackets.OfType<SayiPacket>().Any(s => s.Message == message));

        private void AFiveSecondBarEndingInPsl1ShouldStart()
        {
            var delay = _session.LastPackets.OfType<PdelayPacket>().Single();
            Assert.AreEqual(5000, delay.Delay);
            Assert.AreEqual(DelayPacketType.Locomotion, delay.Type);
            Assert.AreEqual(PslPacketType.Wear, ((PslPacket)delay.Packet.NotNull()).Type);
        }

        private void NoBarShouldStart() => Assert.IsFalse(_session.LastPackets.OfType<PdelayPacket>().Any());

        private void ThePartnerShouldDance() =>
            Assert.IsTrue(Broadcast().OfType<GuriPacket>()
                .Any(s => s.Type == GuriPacketType.Dance && s.EntityId == _partner.MateTransportId));

        private void ThePartnerShouldNotBeTransformed()
        {
            Assert.IsNull(_partner.SpecialistMorph);
            Assert.IsFalse(Broadcast().OfType<CModePacket>().Any());
        }

        private void ThePartnerShouldLookLike_(short morph)
        {
            var cMode = Broadcast().OfType<CModePacket>().Last();
            Assert.AreEqual(_partner.MateTransportId, cMode.VisualId);
            Assert.AreEqual(morph, cMode.Morph);
            Assert.AreEqual(morph, Broadcast().OfType<InPacket>().Last().InNonPlayerSubPacket.NotNull().Morph);
            Assert.AreEqual(morph, _session.LastPackets.OfType<ScnPacket>().Last().MorphId);
        }

        private void TheSkillBarShouldHold_(short[] skills)
        {
            var bar = _session.LastPackets.OfType<PSkiPacket>().Single();
            CollectionAssert.AreEqual(skills, new[] { (short)bar.FirstSkill, (short)bar.SecondSkill, (short)bar.ThirdSkill });
            var scn = _session.LastPackets.OfType<ScnPacket>().Last();
            CollectionAssert.AreEqual(skills, new[] { scn.Skill1Details, scn.Skill2Details, scn.Skill3Details }
                .Select(s => (short)s.NotNull().SkillId).ToArray());
        }

        private void TheOnlookersShouldSeeTheSkills_(short[] skills)
        {
            var sub = Broadcast().OfType<InPacket>().Last().InNonPlayerSubPacket.NotNull();
            CollectionAssert.AreEqual(skills, new[] { sub.Skill1, sub.Skill2, sub.Skill3 });
        }

        private void TheSkillBarShouldBeCleared()
        {
            Assert.AreEqual(0, _session.LastPackets.OfType<DpskiPacket>().Single().State);
            Assert.IsFalse(_session.LastPackets.OfType<PSkiPacket>().Any());
        }

        private void TheTransformationEffectShouldPlay() =>
            Assert.IsTrue(Broadcast().OfType<EffectPacket>()
                .Any(s => s.VisualEntityId == _partner.MateTransportId && s.Id == 196));

        private void TheCooldownShouldBeShownFor_Seconds(int seconds)
        {
            Assert.AreEqual(seconds, _session.LastPackets.OfType<PsdPacket>().Single().SpCooldown);
            Assert.AreEqual(Game18NConstString.DurationOfSideEffect, _session.LastPackets.OfType<SayiPacket>().Last().Message);
        }
    }
}
