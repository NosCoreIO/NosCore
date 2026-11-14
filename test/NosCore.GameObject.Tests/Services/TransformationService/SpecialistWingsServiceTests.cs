//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.TransformationService
{
    [TestClass]
    public class SpecialistWingsServiceTests
    {
        private const short TitanWingsDesign = 6;
        private const short TitanWingsCard = 387;
        private const short ArchangelWingsDesign = 7;
        private const short ArchangelWingsCard = 395;

        private TestWorld World;
        private ClientSession Session;
        private IBuffService Buffs;
        private SpecialistWingsService Service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(c => c.GetSpecialistWingsCard(TitanWingsDesign)).Returns(new CardDto { CardId = TitanWingsCard });
            cards.Setup(c => c.GetSpecialistWingsCard(ArchangelWingsDesign)).Returns(new CardDto { CardId = ArchangelWingsCard });
            cards.Setup(c => c.IsGrantedBy(It.IsIn(TitanWingsCard, ArchangelWingsCard), ItemEffectType.ChangeSpecialistWings))
                .Returns(true);
            cards.Setup(c => c.GetCardBCards(It.IsAny<short>())).Returns([]);
            Buffs = World.Create<BuffService>();
            World.Inject(Buffs);
            Service = World.Create<SpecialistWingsService>();
        }

        [TestMethod]
        public async Task TransformingWithWingsGivesTheirCard()
        {
            await new Spec("Transforming with wings gives their card")
                .Given(CharacterIsTransformedWithDesign_, TitanWingsDesign)
                .WhenAsync(TheWingsAreRefreshed)
                .Then(CharacterShouldOnlyHaveTheCards_, new[] { TitanWingsCard })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChangingWingsSwapsTheirCard()
        {
            await new Spec("Changing wings swaps their card")
                .Given(CharacterIsTransformedWithDesign_, TitanWingsDesign)
                .AndAsync(TheWingsAreRefreshed)
                .And(CharacterIsTransformedWithDesign_, ArchangelWingsDesign)
                .WhenAsync(TheWingsAreRefreshed)
                .Then(CharacterShouldOnlyHaveTheCards_, new[] { ArchangelWingsCard })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeavingTheTransformationTakesTheCardAway()
        {
            await new Spec("Leaving the transformation takes the wings' card away")
                .Given(CharacterIsTransformedWithDesign_, TitanWingsDesign)
                .AndAsync(TheWingsAreRefreshed)
                .And(CharacterLeavesTheTransformation)
                .WhenAsync(TheWingsAreRefreshed)
                .Then(CharacterShouldOnlyHaveTheCards_, new short[0])
                .ExecuteAsync();
        }

        private void CharacterIsTransformedWithDesign_(short design)
        {
            Session.Character.UseSp = true;
            Session.Character.MorphDesign = design;
        }

        private void CharacterLeavesTheTransformation()
        {
            Session.Character.UseSp = false;
        }

        private Task TheWingsAreRefreshed() => Service.RefreshAsync(Session.Character);

        private void CharacterShouldOnlyHaveTheCards_(short[] cards)
        {
            CollectionAssert.AreEquivalent(cards, Buffs.GetActiveBuffs(Session.Character).Select(b => b.CardId).ToArray());
        }
    }
}
