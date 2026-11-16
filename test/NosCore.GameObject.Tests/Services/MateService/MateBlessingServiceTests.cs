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
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MateService;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class MateBlessingServiceTests
    {
        private const short BlessingCardId = 840;
        private TestWorld World;
        private ClientSession Session;
        private BuffService Buffs;
        private MateBlessingService Service;
        private Mate Pet;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(c => c.GetCard(BlessingCardId)).Returns(new CardDto { CardId = BlessingCardId, Duration = 3000 });
            cards.Setup(c => c.GetCardBCards(It.IsAny<short>())).Returns(new List<BCardDto>());
            Buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(Buffs);
            Service = World.Create<MateBlessingService>();
        }

        [TestMethod]
        public async Task APetOutBesideItsOwnerGrantsItsBlessing()
        {
            await new Spec("A pet out beside its owner grants its blessing")
                .GivenAsync(APetGrantingABlessingIsOut_InTheTeam, true)
                .WhenAsync(TheOwnerIsBlessed)
                .Then(TheOwnerHasTheBlessing_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APetLeftOutOfTheTeamGrantsNothing()
        {
            await new Spec("A pet that is not in the team grants nothing")
                .GivenAsync(APetGrantingABlessingIsOut_InTheTeam, false)
                .WhenAsync(TheOwnerIsBlessed)
                .Then(TheOwnerHasTheBlessing_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnExpiredBlessingIsRenewedWhileThePetIsOut()
        {
            await new Spec("A blessing that runs out is renewed while the pet is still out")
                .GivenAsync(APetGrantingABlessingIsOut_InTheTeam, true)
                .WhenAsync(TheBlessingRunsOut)
                .Then(TheOwnerHasTheBlessing_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherCardRunningOutRenewsNothing()
        {
            await new Spec("Another card running out does not renew the blessing")
                .GivenAsync(APetGrantingABlessingIsOut_InTheTeam, true)
                .WhenAsync(AnotherCardRunsOut)
                .Then(TheOwnerHasTheBlessing_, false)
                .ExecuteAsync();
        }

        private async Task APetGrantingABlessingIsOut_InTheTeam(bool inTeam)
        {
            Pet = TestMates.Pet(Session.Character.CharacterId, inTeam);
            Pet.NpcMonster.OwnerBlessingCardId = BlessingCardId;
            Session.Character.Mates[Pet.MateTransportId] = Pet;
            await Pet.SpawnAsync(Session.Character, Session.Character.MapInstance,
                Session.Character.PositionX, Session.Character.PositionY);
        }

        private Task TheOwnerIsBlessed() => Service.BlessAsync(Session.Character);

        private Task TheBlessingRunsOut() => Service.RenewAsync(Session.Character, BlessingCardId);

        private Task AnotherCardRunsOut() => Service.RenewAsync(Session.Character, 841);

        private void TheOwnerHasTheBlessing_(bool expected) =>
            Assert.AreEqual(expected, Buffs.GetActiveBuffs(Session.Character).Any(b => b.CardId == BlessingCardId));
    }
}
