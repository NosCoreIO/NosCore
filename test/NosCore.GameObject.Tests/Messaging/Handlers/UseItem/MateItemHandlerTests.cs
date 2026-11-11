//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.MateExperienceService;
using NosCore.Core.Services.IdService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Mate = NosCore.GameObject.Services.MateService.Mate;
using MateServiceImpl = NosCore.GameObject.Services.MateService.MateService;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class MateItemHandlerTests
    {
        private const short ItemVNum = 2158;
        private const short PartnerVNum = 317;
        private const short BobSkin = 1100;

        private ClientSession _session = null!;
        private MateItemHandler _handler = null!;
        private InventoryItemInstance _item = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _session.Character.Level = 30;
            var mateService = new MateServiceImpl(TestHelpers.Instance.MateDao, new List<NpcMonsterDto>(),
                new IdService<Mate>(1), new MateExperienceService(), NullLogger<MateServiceImpl>.Instance);
            _handler = new MateItemHandler(mateService, new MateExperienceService());
        }

        [TestMethod]
        public async Task FoodRaisesAPetThatIsFarEnoughBelowItsOwnerAsync()
        {
            var pet = Owned(TestMates.Pet(_session.Character.CharacterId, level: 25));
            Holding(TeacherItemEffectType.PetLevelUp);

            await UseOnAsync(pet);

            Assert.AreEqual(26, pet.Level);
            Assert.AreEqual(0, Remaining());
        }

        [TestMethod]
        public async Task FoodIsRefusedForAPetTooCloseToItsOwnerAsync()
        {
            var pet = Owned(TestMates.Pet(_session.Character.CharacterId, level: 26));
            Holding(TeacherItemEffectType.PetLevelUp);

            await UseOnAsync(pet);

            Assert.AreEqual(26, pet.Level);
            Assert.AreEqual(1, Remaining());
            Assert.AreEqual(Game18NConstString.TooLowLevelToUpPet, LastMessage());
        }

        [TestMethod]
        public async Task PetFoodDoesNothingForAPartnerAsync()
        {
            var partner = Owned(TestMates.Partner(_session.Character.CharacterId));
            Holding(TeacherItemEffectType.PetLevelUp);

            await UseOnAsync(partner);

            Assert.AreEqual(1, partner.Level);
            Assert.AreEqual(1, Remaining());
        }

        [TestMethod]
        public async Task PartnerMedicineRaisesAPartnerAsync()
        {
            var partner = Owned(TestMates.Partner(_session.Character.CharacterId));
            Holding(TeacherItemEffectType.PartnerLevelUp);

            await UseOnAsync(partner);

            Assert.AreEqual(2, partner.Level);
            Assert.AreEqual(0, Remaining());
        }

        [TestMethod]
        public async Task AnItemWithoutASelectedMateIsKeptAsync()
        {
            Owned(TestMates.Pet(_session.Character.CharacterId, level: 5));
            Holding(TeacherItemEffectType.PetLevelUp);

            await UseAsync(VisualType.Player, _session.Character.CharacterId);

            Assert.AreEqual(1, Remaining());
        }

        [TestMethod]
        public async Task PickUpTrainingIsTaughtOnceAsync()
        {
            var pet = Owned(TestMates.Pet(_session.Character.CharacterId));
            Holding(TeacherItemEffectType.PetPickUpTraining, amount: 2);

            await UseOnAsync(pet);
            Assert.IsTrue(pet.CanPickUp);
            Assert.AreEqual(Game18NConstString.PetCollectItems, LastMessage());

            await UseOnAsync(pet);
            Assert.AreEqual(1, Remaining(), "a pet that already picks up must not eat a second lesson");
        }

        [TestMethod]
        public async Task SummoningBookMakesAMateSummonableOnceAsync()
        {
            var pet = Owned(TestMates.Pet(_session.Character.CharacterId));
            pet.IsSummonable = false;
            Holding(TeacherItemEffectType.MateSummoningBook, amount: 2);

            await UseOnAsync(pet);
            Assert.IsTrue(pet.IsSummonable);
            Assert.AreEqual(Game18NConstString.UsedMateSummoningBook, LastMessage());

            await UseOnAsync(pet);
            Assert.AreEqual(1, Remaining());
            Assert.AreEqual(Game18NConstString.SummoningBookAlreadyUsed, LastMessage());
        }

        [TestMethod]
        public async Task APetInTheTeamCannotBeReleasedAsync()
        {
            var pet = Owned(TestMates.Pet(_session.Character.CharacterId));
            Holding(TeacherItemEffectType.ReleasePet);

            await UseOnAsync(pet);

            Assert.IsTrue(_session.Character.Mates.ContainsKey(pet.MateTransportId));
            Assert.AreEqual(1, Remaining());
            Assert.AreEqual(Game18NConstString.CanNotReleasePetInYourTeam, LastMessage());
        }

        [TestMethod]
        public async Task APetAtHomeIsReleasedAsync()
        {
            var pet = Owned(TestMates.Pet(_session.Character.CharacterId, isTeamMember: false));
            Holding(TeacherItemEffectType.ReleasePet);

            await UseOnAsync(pet);

            Assert.IsFalse(_session.Character.Mates.ContainsKey(pet.MateTransportId));
            Assert.AreEqual(0, Remaining());
            Assert.IsTrue(_session.LastPackets.OfType<PclearPacket>().Any(), "the pet windows keep a released pet until cleared");
        }

        [TestMethod]
        public async Task APartnerWearingEquipmentCannotBeReleasedAsync()
        {
            var partner = Owned(TestMates.Partner(_session.Character.CharacterId, isTeamMember: false));
            Wearing(NoscorePocketType.FirstPartnerInventory);
            Holding(TeacherItemEffectType.ReleasePartner);

            await UseOnAsync(partner);

            Assert.IsTrue(_session.Character.Mates.ContainsKey(partner.MateTransportId));
            Assert.AreEqual(Game18NConstString.ReleasePartnerWithoutEquipmentOnly, LastMessage());
        }

        [TestMethod]
        public async Task ThePartnerAfterAReleasedOneKeepsItsEquipmentAsync()
        {
            var first = Owned(TestMates.Partner(_session.Character.CharacterId, isTeamMember: false));
            var second = Owned(TestMates.Partner(_session.Character.CharacterId, isTeamMember: false));
            second.PetSlot = 1;
            var gear = Wearing(NoscorePocketType.SecondPartnerInventory);
            Holding(TeacherItemEffectType.ReleasePartner);

            await UseOnAsync(first);

            Assert.AreEqual(0, second.PetSlot);
            Assert.AreEqual(NoscorePocketType.FirstPartnerInventory, gear.Type,
                "slots are given out again by order on login, so the gear has to follow its partner");
        }

        [TestMethod]
        public async Task AFittingCostumeChangesThePartnersLookAsync()
        {
            var partner = Owned(TestMates.Partner(_session.Character.CharacterId));
            Holding(ItemType.Special, ItemEffectType.ApplySkinPartner, PartnerVNum, BobSkin);

            await UseOnAsync(partner);

            Assert.AreEqual(BobSkin, partner.Skin);
            Assert.AreEqual(0, Remaining());
        }

        [TestMethod]
        public async Task ACostumeForAnotherPartnerIsRefusedAsync()
        {
            var partner = Owned(TestMates.Partner(_session.Character.CharacterId));
            Holding(ItemType.Special, ItemEffectType.ApplySkinPartner, 318, 1101);

            await UseOnAsync(partner);

            Assert.AreEqual(0, partner.Skin);
            Assert.AreEqual(1, Remaining());
            Assert.AreEqual(Game18NConstString.CostumeNotFit, LastMessage());
        }

        [TestMethod]
        public async Task APartnerAlreadyInCostumeKeepsItAsync()
        {
            var partner = Owned(TestMates.Partner(_session.Character.CharacterId));
            partner.Skin = BobSkin;
            Holding(ItemType.Special, ItemEffectType.ApplySkinPartner, PartnerVNum, 1199);

            await UseOnAsync(partner);

            Assert.AreEqual(BobSkin, partner.Skin);
            Assert.AreEqual(1, Remaining());
        }

        private Mate Owned(Mate mate)
        {
            _session.Character.Mates[mate.MateTransportId] = mate;
            return mate;
        }

        private void Holding(TeacherItemEffectType effect, short amount = 1) =>
            Holding(ItemType.Teacher, (ItemEffectType)effect, amount: amount);

        private void Holding(ItemType type, ItemEffectType effect, int value = 0, short morph = 0, short amount = 1)
        {
            var item = new Item
            {
                VNum = ItemVNum,
                Type = NoscorePocketType.Etc,
                ItemType = type,
                Effect = effect,
                EffectValue = value,
                Morph = morph
            };
            var inst = new ItemInstanceForTest(ItemVNum) { Amount = amount, Item = item };
            _item = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            _item.Slot = 0;
            _item.Type = NoscorePocketType.Etc;
            _session.Character.InventoryService[_item.ItemInstanceId] = _item;
        }

        private InventoryItemInstance Wearing(NoscorePocketType pocket)
        {
            const short gearVNum = 990;
            var inst = new ItemInstanceForTest(gearVNum) { Amount = 1 };
            var gear = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            gear.Slot = (short)EquipmentType.MainWeapon;
            gear.Type = pocket;
            _session.Character.InventoryService[gear.ItemInstanceId] = gear;
            return gear;
        }

        private Task UseOnAsync(Mate mate) => UseAsync(VisualType.Npc, mate.MateTransportId);

        private Task UseAsync(VisualType visualType, long visualId) =>
            _handler.Handle(new ItemUsedEvent(_session, _item, new UseItemPacket
            {
                VisualType = visualType,
                VisualId = visualId,
                Type = PocketType.Etc,
                Slot = 0,
                Mode = 0,
                Parameter = 0
            }));

        private int Remaining() => _session.Character.InventoryService.CountItem(ItemVNum);

        private Game18NConstString? LastMessage() =>
            _session.LastPackets.OfType<MsgiPacket>().LastOrDefault()?.Message;

        private sealed class ItemInstanceForTest(short vnum) : ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Equipment };
            public object Clone() => MemberwiseClone();
        }
    }
}
