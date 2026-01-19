//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class NcifPacketHandlerTests
    {
        private NcifPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();

            var logger = new Mock<ILogger>().Object;
            Handler = new NcifPacketHandler(
                logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task NcifForPlayerShouldReturnStatInfo()
        {
            await new Spec("Ncif for player should return stat info")
                .Given(CharacterIsOnMap)
                .WhenAsync(RequestingPlayerStatInfo)
                .Then(StInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NcifForUnknownTypeShouldNotSendPacket()
        {
            await new Spec("Ncif for unknown type should not send packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(RequestingUnknownTypeStatInfo)
                .Then(NoStInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NcifForNonExistentEntityShouldNotSendPacket()
        {
            await new Spec("Ncif for non existent entity should not send packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(RequestingNonExistentEntityStatInfo)
                .Then(NoStInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            TestHelpers.Instance.SessionRegistry.Register(new SessionInfo
            {
                ChannelId = Session.Channel!.Id,
                SessionId = Session.SessionId,
                Sender = Session,
                AccountName = Session.Account.Name,
                Disconnect = () => Task.CompletedTask,
                CharacterId = Session.Character.CharacterId,
                MapInstanceId = Session.Character.MapInstance.MapInstanceId,
                Character = Session.Character
            });
        }

        private async Task RequestingPlayerStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = VisualType.Player,
                TargetId = Session.Character.VisualId
            }, Session);
        }

        private async Task RequestingUnknownTypeStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = (VisualType)99,
                TargetId = 1
            }, Session);
        }

        private async Task RequestingNonExistentEntityStatInfo()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new NcifPacket
            {
                Type = VisualType.Player,
                TargetId = 99999
            }, Session);
        }

        private void StInfoPacketShouldBeSent()
        {
            Assert.IsTrue(Session.LastPackets.Any(p => p is StPacket));
        }

        private void NoStInfoPacketShouldBeSent()
        {
            Assert.IsFalse(Session.LastPackets.Any(p => p is StPacket));
        }
    }
}
