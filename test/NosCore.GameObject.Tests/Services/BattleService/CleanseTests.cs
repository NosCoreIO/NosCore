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
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class CleanseTests
    {
        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;
        private int _roll;
        private readonly Dictionary<short, CardDto> _cards = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var catalog = World.Freeze<Mock<ICardCatalog>>();
            catalog.Setup(c => c.GetCard(It.IsAny<short>())).Returns((short id) => _cards.GetValueOrDefault(id));
            catalog.Setup(c => c.GetCardBCards(It.IsAny<short>())).Returns([]);
            World.Freeze<Mock<IRandomProvider>>().Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(() => _roll);
            _buffs = World.Create<BuffService>();
            await ApplyAsync(Card(1, BuffType.Bad, 3));
            await ApplyAsync(Card(2, BuffType.Bad, 5));
            await ApplyAsync(Card(3, BuffType.Good, 2));
            await ApplyAsync(Card(4, BuffType.Good, 4));
        }

        private CardDto Card(short id, BuffType type, byte level)
        {
            var card = new CardDto { CardId = id, BuffType = type, Level = level, Duration = 600 };
            _cards[id] = card;
            return card;
        }

        private Task ApplyAsync(CardDto card, params BCardDto[] bCards) => _buffs.ApplyAsync(_session.Character, card, bCards, null);

        private static BCardDto Effect(BCardEffect effect, int first, int second = 0) =>
            new() { Type = effect.Type(), SubType = effect.SubType(), FirstData = first, SecondData = second };

        private short[] Active() => _buffs.GetActiveBuffs(_session.Character).Select(b => b.CardId).OrderBy(i => i).ToArray();

        [TestMethod]
        public async Task ASkillRemovesEveryBadEffectUpToItsLevel()
        {
            await _buffs.InflictCardsAsync(_session.Character, null, [Effect(BCardEffect.LightAndShadowRemoveBadEffects, 4)]);

            CollectionAssert.AreEqual(new short[] { 2, 3, 4 }, Active());
        }

        [TestMethod]
        public async Task ACardThatRemovesBadEffectsDoesSoWhenItLands()
        {
            await ApplyAsync(Card(10, BuffType.Good, 1), Effect(BCardEffect.SpecialisationBuffResistanceRemoveBadEffects, 100, 5));

            CollectionAssert.AreEqual(new short[] { 3, 4, 10 }, Active());
        }

        [TestMethod]
        public async Task AFailedRollRemovesNothing()
        {
            _roll = 99;

            await ApplyAsync(Card(10, BuffType.Good, 1), Effect(BCardEffect.SpecialisationBuffResistanceRemoveBadEffects, 50, 5));

            CollectionAssert.AreEqual(new short[] { 1, 2, 3, 4, 10 }, Active());
        }

        [TestMethod]
        public async Task ACardCanStripGoodBuffsBelowALevel()
        {
            await ApplyAsync(Card(10, BuffType.Bad, 1), Effect(BCardEffect.SpecialisationBuffResistanceRemoveBuffBelowLevel, 100, 4));

            CollectionAssert.AreEqual(new short[] { 1, 2, 4, 10 }, Active());
        }
    }
}
