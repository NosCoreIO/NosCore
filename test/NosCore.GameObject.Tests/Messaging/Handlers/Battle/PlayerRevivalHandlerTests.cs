//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Battle
{
    [TestClass]
    public class PlayerRevivalHandlerTests
    {
        private PlayerRevivalHandler _handler = null!;
        private ClientSession _session = null!;
        private ClientSession _killerSession = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _killerSession = await TestHelpers.Instance.GenerateSessionAsync();
            _handler = new PlayerRevivalHandler(new Mock<ILogger>().Object);
        }

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
            _session.Character.Reput = reput;
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

        private void VictimReputationShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Reput);

        private void KillerReputationShouldBe_(long expected) => Assert.AreEqual(expected, _killerSession.Character.Reput);
    }
}
