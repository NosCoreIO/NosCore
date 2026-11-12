//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class HeatServiceTests
    {
        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;
        private List<MapTypeMapDto> _mapTypes;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(c => c.GetCard(HeatResistance.CardId)).Returns(new CardDto { CardId = HeatResistance.CardId });
            cards.Setup(c => c.GetCard(HeatResistance.BurningCardId))
                .Returns(new CardDto { CardId = HeatResistance.BurningCardId, Propability = 2 });
            cards.Setup(c => c.GetCardBCards(It.IsAny<short>())).Returns(new List<BCardDto>());
            _buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(_buffs);
            _mapTypes = [];
            World.Inject(_mapTypes);
        }

        private HeatService CreateService(params short[] mapTypes)
        {
            foreach (var type in mapTypes)
            {
                _mapTypes.Add(new MapTypeMapDto { MapId = _session.Character.MapInstance.Map.MapId, MapTypeId = type });
            }

            return World.Create<HeatService>();
        }

        [TestMethod]
        public async Task TheBurningSwordHeatsAct52()
        {
            var service = CreateService((short)MapTypeType.Act52);

            await service.RefreshAsync(_session.Character);

            Assert.IsTrue(_buffs.HasBuff(_session.Character, HeatResistance.BurningCardId));
        }

        [TestMethod]
        public async Task OtherMapsStayCool()
        {
            var service = CreateService((short)MapTypeType.Act51);

            await service.RefreshAsync(_session.Character);

            Assert.IsFalse(_buffs.HasBuff(_session.Character, HeatResistance.BurningCardId));
        }

        [TestMethod]
        public async Task HeatResistanceKeepsTheHeatAway()
        {
            var service = CreateService((short)MapTypeType.Act52);
            await _buffs.ApplyStaticAsync(_session.Character, new CardDto { CardId = HeatResistance.CardId }, [],
                Duration.FromMinutes(10));

            await service.RefreshAsync(_session.Character);

            Assert.IsFalse(_buffs.HasBuff(_session.Character, HeatResistance.BurningCardId));
        }

        [TestMethod]
        public async Task LeavingTheHeatCoolsDown()
        {
            var service = CreateService();
            await _buffs.ApplyPermanentAsync(_session.Character, new CardDto { CardId = HeatResistance.BurningCardId }, []);

            await service.RefreshAsync(_session.Character);

            Assert.IsFalse(_buffs.HasBuff(_session.Character, HeatResistance.BurningCardId));
        }

        [TestMethod]
        public async Task TheHeatReturnsWhenTheResistanceRunsOut()
        {
            var service = CreateService((short)MapTypeType.Act52);
            await _buffs.ApplyStaticAsync(_session.Character, new CardDto { CardId = HeatResistance.CardId }, [],
                Duration.FromMinutes(10));
            World.Clock.Advance(Duration.FromMinutes(11));
            await _buffs.TickAsync(_session.Character);

            await service.RefreshAsync(_session.Character);

            Assert.IsTrue(_buffs.HasBuff(_session.Character, HeatResistance.BurningCardId));
        }
    }
}
