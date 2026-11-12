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
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Database.Entities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class StaticBuffServiceTests
    {
        private const short BlessingCardId = 121;
        private const short CoupleCardId = 319;

        private static readonly IReadOnlyList<BCardDto> BlessingBCards =
        [
            new BCardDto { CardId = BlessingCardId, Type = 44, SubType = 11, FirstData = 100 }
        ];

        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;
        private StaticBuffService _service;
        private IDao<StaticBuffDto, long> _staticBuffDao;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(c => c.GetCard(BlessingCardId)).Returns(Blessing);
            cards.Setup(c => c.GetCardBCards(BlessingCardId)).Returns(BlessingBCards);
            _buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(_buffs);
            _staticBuffDao = new Dao<StaticBuff, StaticBuffDto, long>(NullLogger<Dao<StaticBuff, StaticBuffDto, long>>.Instance,
                World.DbContextBuilder);
            World.Inject(_staticBuffDao);
            _service = World.Create<StaticBuffService>();
        }

        private static CardDto Blessing => new() { CardId = BlessingCardId, Duration = 6001 };

        private long CharacterId => _session.Character.CharacterId;

        private List<StaticBuffDto> Stored() =>
            _staticBuffDao.Where(s => s.CharacterId == CharacterId)?.ToList() ?? [];

        private Task BlessAsync(Duration duration) =>
            _buffs.ApplyStaticAsync(_session.Character, Blessing, BlessingBCards, duration);

        [TestMethod]
        public async Task ABlessingIsStoredWithTheSecondsItHasLeft()
        {
            await BlessAsync(Duration.FromHours(1));
            World.Clock.Advance(Duration.FromMinutes(10));

            Assert.IsTrue(await _service.SaveAsync(_session.Character));

            var row = Stored().Single();
            Assert.AreEqual(BlessingCardId, row.CardId);
            Assert.AreEqual(3000, row.RemainingTime);
        }

        [TestMethod]
        public async Task SavingAgainUpdatesTheSameRow()
        {
            await BlessAsync(Duration.FromHours(1));
            await _service.SaveAsync(_session.Character);
            var first = Stored().Single().StaticBuffId;
            World.Clock.Advance(Duration.FromMinutes(1));

            await _service.SaveAsync(_session.Character);

            var row = Stored().Single();
            Assert.AreEqual(first, row.StaticBuffId);
            Assert.AreEqual(3540, row.RemainingTime);
        }

        [TestMethod]
        public async Task ABlessingThatRanOutIsNoLongerStored()
        {
            await BlessAsync(Duration.FromHours(1));
            await _service.SaveAsync(_session.Character);
            World.Clock.Advance(Duration.FromHours(1) + Duration.FromSeconds(1));

            Assert.IsTrue(await _service.SaveAsync(_session.Character));

            Assert.AreEqual(0, Stored().Count);
        }

        [TestMethod]
        public async Task ABuffWithoutAnEndIsNotStored()
        {
            await _buffs.ApplyPermanentAsync(_session.Character, new CardDto { CardId = CoupleCardId }, []);

            Assert.IsTrue(await _service.SaveAsync(_session.Character));

            Assert.AreEqual(0, Stored().Count);
        }

        [TestMethod]
        public async Task AnOrdinaryBuffIsNotStored()
        {
            await _buffs.ApplyAsync(_session.Character, Blessing, BlessingBCards, null);

            Assert.IsTrue(await _service.SaveAsync(_session.Character));

            Assert.AreEqual(0, Stored().Count);
        }

        [TestMethod]
        public async Task TheBlessingComesBackWithTheTimeItHadLeft()
        {
            await _staticBuffDao.TryInsertOrUpdateAsync(new StaticBuffDto
            {
                CharacterId = CharacterId, CardId = BlessingCardId, RemainingTime = 3000
            });

            await _service.RestoreAsync(_session.Character);

            var buff = _buffs.GetActiveBuffs(_session.Character).Single(s => s.CardId == BlessingCardId);
            Assert.IsTrue(buff.Static);
            Assert.AreEqual(3000, (int)(buff.ExpiresAt - World.Clock.GetCurrentInstant()).TotalSeconds);
            Assert.AreSame(BlessingBCards, buff.BCards);
            var vb = _session.LastPackets.OfType<VbPacket>().Last();
            Assert.AreEqual(BlessingCardId, vb.CardId);
            Assert.IsTrue(vb.IsActivated);
            Assert.AreEqual(30000, vb.RemainingTime);
        }

        [TestMethod]
        public async Task TheTimeDoesNotRunWhileTheCharacterIsAway()
        {
            await BlessAsync(Duration.FromHours(1));
            World.Clock.Advance(Duration.FromMinutes(10));
            await _service.SaveAsync(_session.Character);
            await _buffs.RemoveAsync(_session.Character, BlessingCardId);
            World.Clock.Advance(Duration.FromHours(5));

            await _service.RestoreAsync(_session.Character);

            var buff = _buffs.GetActiveBuffs(_session.Character).Single(s => s.CardId == BlessingCardId);
            Assert.AreEqual(3000, (int)(buff.ExpiresAt - World.Clock.GetCurrentInstant()).TotalSeconds);
        }

        [TestMethod]
        public async Task ARowForACardThatNoLongerExistsIsLeftOut()
        {
            await _staticBuffDao.TryInsertOrUpdateAsync(new StaticBuffDto
            {
                CharacterId = CharacterId, CardId = 9999, RemainingTime = 3000
            });

            await _service.RestoreAsync(_session.Character);

            Assert.AreEqual(0, _buffs.GetActiveBuffs(_session.Character).Count);
        }

        [TestMethod]
        public async Task SomeoneElsesBlessingIsNotRestored()
        {
            await _staticBuffDao.TryInsertOrUpdateAsync(new StaticBuffDto
            {
                CharacterId = CharacterId + 1, CardId = BlessingCardId, RemainingTime = 3000
            });

            await _service.RestoreAsync(_session.Character);

            Assert.AreEqual(0, _buffs.GetActiveBuffs(_session.Character).Count);
        }
    }
}
