//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class NosMateBeadHandlerTests
    {
        private const short BeadVNum = 335;
        private const short MonsterVNum = 417;

        private TestWorld World;
        private ClientSession _session;
        private Mock<IMinilandService> _minilands;
        private InventoryItemInstance _bead;
        private NosMateBeadHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _minilands = World.Freeze<Mock<IMinilandService>>();
            var monsters = new List<NpcMonsterDto> { new() { NpcMonsterVNum = MonsterVNum, MaxHp = 500, MaxMp = 200 } };
            World.Inject(monsters);
            World.Inject<IMateService>(new MateService(World.MateDao, monsters, new IdService<Mate>(1),
                World.Create<MateStatsService>(), NullLogger<MateService>.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object));
            _handler = World.Create<NosMateBeadHandler>();
        }

        [TestMethod]
        public async Task TheFirstUseAsksBeforeOpeningTheBead()
        {
            HoldingABead(subType: 1);

            await UseAsync(0);

            Assert.AreEqual(Game18NConstString.AskRemovePartnerFromBead,
                _session.LastPackets.OfType<QnaiPacket>().Single().Question);
            Assert.AreEqual(1, Remaining());
        }

        [TestMethod]
        public async Task ABeadOnlyOpensInTheOwnersMiniland()
        {
            HoldingABead(subType: 0);

            await UseAsync(1);

            Assert.AreEqual(Game18NConstString.MinilandOnly, _session.LastPackets.OfType<MsgiPacket>().Single().Message);
            Assert.AreEqual(1, Remaining());
            Assert.AreEqual(0, _session.Character.Mates.Count);
        }

        [TestMethod]
        public async Task OpeningTheBeadInTheMinilandReleasesTheMate()
        {
            HoldingABead(subType: 1);
            InOwnMiniland();

            await UseAsync(1);

            var mate = _session.Character.Mates.Values.Single();
            Assert.AreEqual(MonsterVNum, mate.VNum);
            Assert.AreEqual(MateType.Partner, mate.MateType);
            Assert.AreEqual(53, mate.Level);
            Assert.AreEqual(0, Remaining());
            Assert.AreEqual(1, World.MateDao.LoadAll().Count());
            Assert.AreEqual(Game18NConstString.PartnerOutOfBead, _session.LastPackets.OfType<MsgiPacket>().Last().Message);
        }

        [TestMethod]
        public async Task AnEmptyBeadAsksBeforeStoringTheSelectedPet()
        {
            var pet = await OwnedAsync(TestMates.Pet(_session.Character.CharacterId, isTeamMember: false));
            HoldingAnEmptyBead(subType: 0);

            await UseOnAsync(pet, 0);

            var question = _session.LastPackets.OfType<QnaiPacket>().Single();
            Assert.AreEqual(Game18NConstString.AskPutNosmateInBead, question.Question);
            var yes = (UseItemPacket)question.YesPacket.NotNull();
            Assert.AreEqual(VisualType.Npc, yes.VisualType);
            Assert.AreEqual(pet.MateTransportId, yes.VisualId);
            Assert.AreEqual(1, yes.Mode);
            Assert.IsTrue(_session.Character.Mates.ContainsKey(pet.MateTransportId));
        }

        [TestMethod]
        public async Task ConfirmingStoresThePetInTheBead()
        {
            var pet = await OwnedAsync(TestMates.Pet(_session.Character.CharacterId, isTeamMember: false, level: 27, vnum: MonsterVNum));
            pet.Attack = 3;
            pet.Defence = 5;
            var bead = HoldingAnEmptyBead(subType: 0);

            await UseOnAsync(pet, 1);

            Assert.IsFalse(_session.Character.Mates.ContainsKey(pet.MateTransportId));
            Assert.AreEqual(0, World.MateDao.LoadAll().Count());
            Assert.AreEqual(MonsterVNum, bead.HoldingVNum);
            Assert.AreEqual(27, bead.SpLevel);
            Assert.AreEqual(3, bead.SpDamage);
            Assert.AreEqual(5, bead.SpDefence);
            Assert.AreEqual(1, Remaining());
            Assert.AreEqual(Game18NConstString.PetIsNowInBead, _session.LastPackets.OfType<MsgiPacket>().Last().Message);
        }

        [TestMethod]
        public async Task APetInTheTeamIsStoredToo()
        {
            var pet = await OwnedAsync(TestMates.Pet(_session.Character.CharacterId));
            var bead = HoldingAnEmptyBead(subType: 0);

            await UseOnAsync(pet, 1);

            Assert.IsFalse(_session.Character.Mates.ContainsKey(pet.MateTransportId));
            Assert.AreEqual((short)333, bead.HoldingVNum);
        }

        [TestMethod]
        public async Task APetBeadDoesNotTakeAPartner()
        {
            var partner = await OwnedAsync(TestMates.Partner(_session.Character.CharacterId, isTeamMember: false));
            var bead = HoldingAnEmptyBead(subType: 0);

            await UseOnAsync(partner, 1);

            Assert.IsTrue(_session.Character.Mates.ContainsKey(partner.MateTransportId));
            Assert.IsNull(bead.HoldingVNum);
            Assert.AreEqual(Game18NConstString.CanNotPutNosmateInBead, _session.LastPackets.OfType<MsgiPacket>().Single().Message);
        }

        [TestMethod]
        public async Task APartnerInACostumeIsWarnedTheCostumeIsLost()
        {
            var partner = await OwnedAsync(TestMates.Partner(_session.Character.CharacterId, isTeamMember: false));
            partner.Skin = 1101;
            HoldingAnEmptyBead(subType: 1);

            await UseOnAsync(partner, 0);

            Assert.AreEqual(Game18NConstString.AskPartnerInBead, _session.LastPackets.OfType<QnaiPacket>().Single().Question);
        }

        [TestMethod]
        public async Task AFullBeadReleasesItsMateWithTheStoredLevelInTheMiniland()
        {
            var bead = HoldingAnEmptyBead(subType: 0);
            bead.HoldingVNum = MonsterVNum;
            bead.SpLevel = 27;
            bead.SpDamage = 3;
            bead.SpDefence = 5;
            InOwnMiniland();

            await UseAsync(1);

            var mate = _session.Character.Mates.Values.Single();
            Assert.AreEqual(MonsterVNum, mate.VNum);
            Assert.AreEqual(MateType.Pet, mate.MateType);
            Assert.AreEqual(27, mate.Level);
            Assert.AreEqual(3, mate.Attack);
            Assert.AreEqual(5, mate.Defence);
            Assert.AreEqual(0, Remaining());
            Assert.AreEqual(Game18NConstString.PetOutOfBead, _session.LastPackets.OfType<MsgiPacket>().Last().Message);
        }

        [TestMethod]
        public async Task AFullBeadAsksBeforeReleasing()
        {
            var bead = HoldingAnEmptyBead(subType: 1);
            bead.HoldingVNum = MonsterVNum;

            await UseAsync(0);

            Assert.AreEqual(Game18NConstString.AskRemovePartnerFromBead,
                _session.LastPackets.OfType<QnaiPacket>().Single().Question);
            Assert.AreEqual(1, Remaining());
        }

        private async Task<Mate> OwnedAsync(Mate mate)
        {
            var saved = await World.MateDao.TryInsertOrUpdateAsync(mate.Adapt<MateDto>());
            mate.MateId = saved.NotNull().MateId;
            _session.Character.Mates[mate.MateTransportId] = mate;
            return mate;
        }

        private BoxInstance HoldingAnEmptyBead(byte subType)
        {
            var item = new Item
            {
                VNum = BeadVNum,
                Type = NoscorePocketType.Equipment,
                ItemType = ItemType.Box,
                ItemSubType = subType,
                Effect = (ItemEffectType)BoxItemEffectType.NosMateHolder,
            };
            var instance = new BoxInstance(item) { Amount = 1 };
            _bead = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _bead.Slot = 0;
            _bead.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[_bead.ItemInstanceId] = _bead;
            return instance;
        }

        private Task UseOnAsync(Mate mate, byte mode) => _handler.Handle(new ItemUsedEvent(_session, _bead, new UseItemPacket
        {
            VisualType = VisualType.Npc,
            VisualId = mate.MateTransportId,
            Type = _bead.Type.ToPocketType(),
            Slot = 0,
            Mode = mode,
        }));

        private void InOwnMiniland()
        {
            _session.Character.MapInstance = World.GetMap(World.MinilandId);
            _minilands.Setup(m => m.GetMinilandFromMapInstanceId(World.MinilandId))
                .Returns(new Miniland { MapInstanceId = World.MinilandId, OwnerId = _session.Character.CharacterId });
        }

        private void HoldingABead(byte subType)
        {
            var item = new Item
            {
                VNum = BeadVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Box,
                ItemSubType = subType,
                Effect = (ItemEffectType)BoxItemEffectType.NosMateBead,
                EffectValue = MonsterVNum,
                LevelMinimum = 53,
            };
            var instance = new ItemInstanceForTest(BeadVNum) { Amount = 1, Item = item };
            _bead = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _bead.Slot = 0;
            _bead.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_bead.ItemInstanceId] = _bead;
        }

        private Task UseAsync(byte mode) => _handler.Handle(new ItemUsedEvent(_session, _bead, new UseItemPacket
        {
            VisualType = VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = _bead.Type.ToPocketType(),
            Slot = 0,
            Mode = mode,
        }));

        private int Remaining() => _session.Character.InventoryService.CountItem(BeadVNum);

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
