//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.PenaltyService;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class PenaltyCommandTests
    {
        private TestWorld World;
        private ClientSession _moderator;
        private ClientSession _target;
        private IPenaltyService _penalties;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _moderator = await World.CreateSessionAsync();
            _target = await World.CreateSessionAsync();
            _penalties = World.Create<IPenaltyService>();
        }

        [TestMethod]
        public async Task MutingSilencesTheAccountForTheGivenMinutes()
        {
            await World.Create<MutePacketHandler>().ExecuteAsync(
                new MutePacket { Name = _target.Character.Name, Minutes = 30, Reason = "spam" }, _moderator);

            Assert.AreEqual(Duration.FromMinutes(30), await _penalties.GetMuteLeftAsync(_target.Account.AccountId));
            var log = World.PenaltyLogDao.Where(p => p.AccountId == _target.Account.AccountId)?.Single();
            Assert.AreEqual("spam", log?.Reason);
            Assert.AreEqual(_moderator.Character.Name, log?.AdminName);
        }

        [TestMethod]
        public async Task TheModeratorIsToldWhoWasMutedAndForHowLong()
        {
            await World.Create<MutePacketHandler>().ExecuteAsync(
                new MutePacket { Name = _target.Character.Name, Minutes = 30 }, _moderator);

            var told = _moderator.LastPackets.OfType<Msgi2Packet>().Single();
            Assert.AreEqual(Game18NConstString.HasBeenPunished, told.Message);
            Assert.AreEqual(_target.Character.Name, told.Game18NArguments[0]);
            Assert.AreEqual(30, told.Game18NArguments[1]);
        }

        [TestMethod]
        public async Task UnmutingLetsTheAccountSpeakAgain()
        {
            await World.Create<MutePacketHandler>().ExecuteAsync(
                new MutePacket { Name = _target.Character.Name, Minutes = 30 }, _moderator);

            await World.Create<UnmutePacketHandler>().ExecuteAsync(new UnmutePacket { Name = _target.Character.Name }, _moderator);

            Assert.IsNull(await _penalties.GetMuteLeftAsync(_target.Account.AccountId));
        }

        [TestMethod]
        public async Task BanningRefusesTheAccountAndDisconnectsIt()
        {
            await World.Create<BanPacketHandler>().ExecuteAsync(
                new BanPacket { Name = _target.Character.Name, Days = 3 }, _moderator);

            Assert.IsTrue(await _penalties.IsBannedAsync(_target.Account.AccountId));
            World.PubSubHub.Verify(s => s.DisconnectAccountAsync(_target.Account.Name), Times.Once);
            World.Clock.Advance(Duration.FromDays(3));
            Assert.IsFalse(await _penalties.IsBannedAsync(_target.Account.AccountId));
        }

        [TestMethod]
        public async Task ABanOfZeroDaysNeverEnds()
        {
            await World.Create<BanPacketHandler>().ExecuteAsync(
                new BanPacket { Name = _target.Character.Name, Days = 0 }, _moderator);

            World.Clock.Advance(Duration.FromDays(36500));
            Assert.IsTrue(await _penalties.IsBannedAsync(_target.Account.AccountId));
        }

        [TestMethod]
        public async Task UnbanningLetsTheAccountBackIn()
        {
            await World.Create<BanPacketHandler>().ExecuteAsync(
                new BanPacket { Name = _target.Character.Name, Days = 0 }, _moderator);

            await World.Create<UnbanPacketHandler>().ExecuteAsync(new UnbanPacket { Name = _target.Character.Name }, _moderator);

            Assert.IsFalse(await _penalties.IsBannedAsync(_target.Account.AccountId));
        }

        [TestMethod]
        public async Task UnbanningLeavesAMuteInPlace()
        {
            await World.Create<MutePacketHandler>().ExecuteAsync(
                new MutePacket { Name = _target.Character.Name, Minutes = 30 }, _moderator);

            await World.Create<UnbanPacketHandler>().ExecuteAsync(new UnbanPacket { Name = _target.Character.Name }, _moderator);

            Assert.IsNotNull(await _penalties.GetMuteLeftAsync(_target.Account.AccountId));
        }

        [TestMethod]
        public async Task AnUnknownNameIsRefusedAndNothingIsWritten()
        {
            await World.Create<BanPacketHandler>().ExecuteAsync(new BanPacket { Name = "Nobody", Days = 1 }, _moderator);
            await World.Create<MutePacketHandler>().ExecuteAsync(new MutePacket { Name = "Nobody", Minutes = 1 }, _moderator);

            Assert.AreEqual(2, _moderator.LastPackets.OfType<InfoiPacket>().Count(p => p.Message == Game18NConstString.UnknownCharacter));
            Assert.AreEqual(0, World.PenaltyLogDao.Where(_ => true)?.Count() ?? 0);
            World.PubSubHub.Verify(s => s.DisconnectAccountAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
