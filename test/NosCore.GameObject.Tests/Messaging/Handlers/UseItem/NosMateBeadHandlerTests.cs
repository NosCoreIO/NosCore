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
using Moq;
using NosCore.Core.Services.IdService;
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
            Type = PocketType.Main,
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
