//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class BuffGroupTests
    {
        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;
        private readonly Dictionary<short, CardDto> _cards = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var catalog = World.Freeze<Mock<ICardCatalog>>();
            catalog.Setup(c => c.GetCard(It.IsAny<short>())).Returns((short id) => _cards.GetValueOrDefault(id));
            _buffs = World.Create<BuffService>();
        }

        private CardDto Card(short id, short group, BuffType type = BuffType.Good, byte level = 1)
        {
            var card = new CardDto { CardId = id, GroupId = group, BuffType = type, Level = level, Duration = 600 };
            _cards[id] = card;
            return card;
        }

        private Task ApplyAsync(CardDto card) => _buffs.ApplyAsync(_session.Character, card, [], null);

        private short[] Active() => _buffs.GetActiveBuffs(_session.Character).Select(b => b.CardId).OrderBy(i => i).ToArray();

        [TestMethod]
        public async Task ABuffOfTheSameFamilyReplacesTheActiveOne()
        {
            await ApplyAsync(Card(374, 428));

            await ApplyAsync(Card(377, 428));

            CollectionAssert.AreEqual(new short[] { 377 }, Active());
        }

        [TestMethod]
        public async Task BuffsOfOtherFamiliesStay()
        {
            await ApplyAsync(Card(339, 404));

            await ApplyAsync(Card(340, 405));

            CollectionAssert.AreEqual(new short[] { 339, 340 }, Active());
        }

        [TestMethod]
        public async Task FamilyZeroNeverReplaces()
        {
            await ApplyAsync(Card(1, 0));

            await ApplyAsync(Card(21, 0));

            CollectionAssert.AreEqual(new short[] { 1, 21 }, Active());
        }

        [TestMethod]
        public async Task OnlyBadBuffsAreRemovedByADispel()
        {
            await ApplyAsync(Card(1, 0, BuffType.Bad));
            await ApplyAsync(Card(131, 95, BuffType.Good));

            await _buffs.RemoveBadAsync(_session.Character, 5);

            CollectionAssert.AreEqual(new short[] { 131 }, Active());
        }
    }
}
