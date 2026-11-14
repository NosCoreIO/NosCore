//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.FactionWarService;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Battle
{
    [TestClass]
    public class PlayerRevivalHandlerTests
    {
        private TestWorld World;
        private PlayerRevivalHandler _handler;
        private ClientSession _session;
        private ClientSession _killerSession;
        private Mock<IFactionWarService> _factionWar;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _killerSession = await World.CreateSessionAsync();
            _factionWar = World.Freeze<Mock<IFactionWarService>>();
            _handler = World.Create<PlayerRevivalHandler>();
        }

        [TestMethod]
        public async Task AnEnemyKillInGlacernonCountsForTheWarInsteadOfCostingDignity()
        {
            await new Spec("Falling to an enemy in Glacernon costs no dignity and counts toward the faction war")
                .Given(CharacterHasHpMpAndDignity_, 1000, 500, (short)0)
                .And(TheCharacterIsInGlacernon)
                .WhenAsync(PlayerDiesKilledByOtherPlayer)
                .Then(DignityShouldBe_, (short)0)
                .And(TheKillShouldBeRecorded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallInGlacernonWithoutAnEnemyStillCostsDignity()
        {
            await new Spec("Falling in Glacernon to anything but a player costs dignity as anywhere else")
                .Given(CharacterHasHpMpAndDignity_, 1000, 500, (short)0)
                .And(TheCharacterIsInGlacernon)
                .WhenAsync(PlayerDiesWithoutKiller)
                .Then(DignityShouldBe_, (short)-50)
                .And(NoKillShouldBeRecorded)
                .ExecuteAsync();
        }

        private void TheCharacterIsInGlacernon() => _session.Character.MapInstance.IsAct4 = true;

        private void TheKillShouldBeRecorded()
        {
            var killer = _killerSession.Character.CharacterId;
            var victim = _session.Character.CharacterId;
            _factionWar.Verify(w => w.RecordKillAsync(It.Is<PlayerComponentBundle>(p => p.CharacterId == killer),
                It.Is<PlayerComponentBundle>(p => p.CharacterId == victim)), Times.Once);
        }

        private void NoKillShouldBeRecorded() =>
            _factionWar.Verify(w => w.RecordKillAsync(It.IsAny<PlayerComponentBundle>(), It.IsAny<PlayerComponentBundle>()), Times.Never);

        [TestMethod]
        public async Task NonPlayerVictimIsIgnored()
        {
            var victim = new Mock<IAliveEntity>();
            victim.SetupGet(v => v.VisualType).Returns(VisualType.Monster);
            await _handler.Handle(new EntityDiedEvent(victim.Object, null));
        }

        [TestMethod]
        public async Task PlayerDeathZeroesHpMpAndDecrementsDignityBy50()
        {
            await new Spec("Player death sets HP/MP=0 and applies the -50 dignity penalty")
                .Given(CharacterHasHpMpAndDignity_, 1000, 500, (short)0)
                .WhenAsync(PlayerDiesWithoutKiller)
                .Then(HpShouldBe_, 0)
                .And(MpShouldBe_, 0)
                .And(DignityShouldBe_, (short)-50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DignityClampsAtMinusOneThousandFloor()
        {
            await new Spec("Dignity never drops below -1000 on repeated deaths")
                .Given(CharacterHasHpMpAndDignity_, 1000, 500, (short)-980)
                .WhenAsync(PlayerDiesWithoutKiller)
                .Then(DignityShouldBe_, (short)-1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LevelAtOrBelow20GetsFreeReviveDialog()
        {
            await new Spec("Level<=20 death sends the ContinueHereFree dialog")
                .Given(CharacterHasHpMpAndDignity_, 1000, 500, (short)0)
                .And(CharacterLevelIs_, (byte)20)
                .WhenAsync(PlayerDiesWithoutKiller)
                .Then(DialogQuestionShouldBe_, Game18NConstString.ContinueHereFree)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LevelAbove20GetsTenSeedsReviveDialog()
        {
            await new Spec("Level>20 death sends the ContinueHereTenSeeds dialog")
                .Given(CharacterHasHpMpAndDignity_, 1000, 500, (short)0)
                .And(CharacterLevelIs_, (byte)21)
                .WhenAsync(PlayerDiesWithoutKiller)
                .Then(DialogQuestionShouldBe_, Game18NConstString.ContinueHereTenSeeds)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KilledByOtherPlayerWithHighReputTransfersPortion()
        {
            await new Spec("PK victim with Reput>=50000 transfers level*50 reputation to the killer")
                .Given(CharacterHasHpMpAndDignity_, 1000, 500, (short)0)
                .And(CharacterLevelIs_, (byte)10)
                .And(VictimHasReputation_, 60000L)
                .WhenAsync(PlayerDiesKilledByOtherPlayer)
                .Then(VictimReputationShouldBe_, 60000L - 10 * 50L)
                .And(KillerReputationShouldBe_, 10 * 50L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task KilledByOtherPlayerWithLowReputDoesNotTransfer()
        {
            await new Spec("PK victim with Reput<50000 does not transfer reputation")
                .Given(CharacterHasHpMpAndDignity_, 1000, 500, (short)0)
                .And(CharacterLevelIs_, (byte)10)
                .And(VictimHasReputation_, 40000L)
                .WhenAsync(PlayerDiesKilledByOtherPlayer)
                .Then(VictimReputationShouldBe_, 40000L)
                .And(KillerReputationShouldBe_, 0L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnArenaFallOffersThePaidRevive()
        {
            await new Spec("Falling in an arena asks whether to revive on the spot (revival 2) or go home (revival 1)")
                .Given(CharacterHasHpMpAndDignity_, 1000, 500, (short)0)
                .And(TheCharacterIsInAnArena)
                .WhenAsync(PlayerDiesKilledByOtherPlayer)
                .Then(DialogQuestionShouldBe_, Game18NConstString.AskRevive)
                .And(TheDialogShouldAnswerWith_And_, RevivalType.InArena, RevivalType.AtSavePoint)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnArenaFallCostsNeitherDignityNorReputation()
        {
            await new Spec("An arena fall leaves dignity and reputation untouched")
                .Given(CharacterHasHpMpAndDignity_, 1000, 500, (short)0)
                .And(CharacterLevelIs_, (byte)10)
                .And(VictimHasReputation_, 60000L)
                .And(TheCharacterIsInAnArena)
                .WhenAsync(PlayerDiesKilledByOtherPlayer)
                .Then(DignityShouldBe_, (short)0)
                .And(VictimReputationShouldBe_, 60000L)
                .And(KillerReputationShouldBe_, 0L)
                .And(HpShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFallInAnInstantCombatIsOutOfIt()
        {
            await new Spec("Falling in an Instant Combat revives at 1 HP without asking, and costs no dignity")
                .Given(CharacterHasHpMpAndDignity_, 1000, 500, (short)0)
                .And(TheCharacterIsInAnInstantCombat)
                .WhenAsync(PlayerDiesWithoutKiller)
                .Then(HpShouldBe_, 1)
                .And(MpShouldBe_, 1)
                .And(DignityShouldBe_, (short)0)
                .And(NoRevivalQuestionShouldBeAsked)
                .ExecuteAsync();
        }

        private void TheCharacterIsInAnInstantCombat() =>
            _session.Character.MapInstance.MapInstanceType = MapInstanceType.InstantCombatInstance;

        private void NoRevivalQuestionShouldBeAsked() =>
            Assert.IsFalse(_session.LastPackets.OfType<DlgiPacket>().Any());

        private void TheCharacterIsInAnArena() =>
            _session.Character.MapInstance.MapInstanceType = MapInstanceType.ArenaInstance;

        private void TheDialogShouldAnswerWith_And_(RevivalType yes, RevivalType no)
        {
            var dlg = _session.LastPackets.OfType<DlgiPacket>().First();
            Assert.AreEqual(yes, ((RevivalPacket)dlg.YesPacket.NotNull()).Type);
            Assert.AreEqual(no, ((RevivalPacket)dlg.NoPacket.NotNull()).Type);
        }

        private void CharacterHasHpMpAndDignity_(int hp, int mp, short dignity)
        {
            _session.Character.Hp = hp;
            _session.Character.Mp = mp;
            _session.Character.Dignity = dignity;
        }

        private void CharacterLevelIs_(byte level)
        {
            _session.Character.Level = level;
        }

        private void VictimHasReputation_(long reput)
        {
            _session.Character.Reputation = reput;
        }

        private async Task PlayerDiesWithoutKiller()
        {
            await _handler.Handle(new EntityDiedEvent(_session.Character, null));
        }

        private async Task PlayerDiesKilledByOtherPlayer()
        {
            await _handler.Handle(new EntityDiedEvent(_session.Character, _killerSession.Character));
        }

        private void HpShouldBe_(int expected) => Assert.AreEqual(expected, _session.Character.Hp);

        private void MpShouldBe_(int expected) => Assert.AreEqual(expected, _session.Character.Mp);

        private void DignityShouldBe_(short expected) => Assert.AreEqual(expected, _session.Character.Dignity);

        private void DialogQuestionShouldBe_(Game18NConstString expected)
        {
            var dlg = _session.LastPackets.OfType<DlgiPacket>().FirstOrDefault();
            Assert.IsNotNull(dlg);
            Assert.AreEqual(expected, dlg.Question);
        }

        private void VictimReputationShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Reputation);

        private void KillerReputationShouldBe_(long expected) => Assert.AreEqual(expected, _killerSession.Character.Reputation);
    }
}
