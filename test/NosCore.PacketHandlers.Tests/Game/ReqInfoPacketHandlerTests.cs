//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class ReqInfoPacketHandlerTests
    {
        private ReqInfoPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            TestHelpers.Instance.SessionRegistry.Register(new SessionInfo
            {
                ChannelId = Session.Channel!.Id,
                SessionId = Session.SessionId,
                Sender = Session,
                AccountName = Session.Account.Name,
                Disconnect = () => Task.CompletedTask,
                CharacterId = Session.Character.CharacterId,
                MapInstanceId = Session.Character.MapInstance.MapInstanceId,
            });

            Handler = new ReqInfoPacketHandler(
                new Mock<ILogger>().Object,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task PlayerReqInfoRepliesWithTcInfoForTheTargetedCharacter()
        {
            await new Spec("req_info on a player target replies with tc_info")
                .WhenAsync(RequestingPlayerInfoForSelf)
                .Then(TcInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayerReqInfoForUnknownTargetEmitsNothing()
        {
            await new Spec("req_info on an unknown player visualId emits nothing")
                .WhenAsync(RequestingPlayerInfoForUnknownVisualId)
                .Then(NoTcInfoShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NpcReqInfoIsLoggedAndIgnoredUntilSubsystemLands()
        {
            // EInfoPacketType has no Npc value in NosCore.Packets yet, and the e_info shape
            // for NPCs is item-oriented in the schema. Documented as a no-op + warning until
            // the packets repo grows the NPC variant.
            await new Spec("req_info on an Npc target is a logged no-op (e_info NPC variant not wired)")
                .WhenAsync(RequestingNpcInfo)
                .Then(NoTcInfoShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MateReqInfoIsLoggedAndIgnoredUntilSubsystemLands()
        {
            await new Spec("req_info on a Mate target is a logged no-op (mate subsystem not implemented)")
                .WhenAsync(RequestingMateInfo)
                .Then(NoTcInfoShouldBeSent)
                .ExecuteAsync();
        }

        // --- Whens ---

        private async Task RequestingPlayerInfoForSelf()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ReqInfoPacket
            {
                ReqType = ReqInfoType.PlayerInfo,
                TargetVNum = Session.Character.VisualId,
            }, Session);
        }

        private async Task RequestingPlayerInfoForUnknownVisualId()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ReqInfoPacket
            {
                ReqType = ReqInfoType.PlayerInfo,
                TargetVNum = 99999,
            }, Session);
        }

        private async Task RequestingNpcInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ReqInfoPacket
            {
                ReqType = ReqInfoType.NpcInfo,
                TargetVNum = 1,
            }, Session);
        }

        private async Task RequestingMateInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ReqInfoPacket
            {
                ReqType = ReqInfoType.MateInfo,
                TargetVNum = 1,
                MateVNum = 1,
            }, Session);
        }

        // --- Thens ---

        private void TcInfoPacketShouldBeSent() =>
            Assert.IsTrue(Session.LastPackets.Any(p => p is TcInfoPacket));

        private void NoTcInfoShouldBeSent() =>
            Assert.IsFalse(Session.LastPackets.Any(p => p is TcInfoPacket));
    }
}
