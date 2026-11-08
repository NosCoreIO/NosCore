//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking.SessionRef;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Networking
{
    [TestClass]
    public class PacketRateLimitTests
    {
        private readonly Dictionary<Type, int> _handled = new();
        private readonly Mock<ILogger<WorldPacketHandlingStrategy>> _logger = new();
        private ClientSession _session = null!;
        private WorldPacketHandlingStrategy _strategy = null!;
        private int _keepAlive;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync(new List<IPacketHandler>
            {
                new Counting<NcifPacket>(_handled),
                new Counting<ClientSayPacket>(_handled),
                new Counting<UseSkillPacket>(_handled),
                new Counting<GuriPacket>(_handled),
            });
            _strategy = new WorldPacketHandlingStrategy(_logger.Object, TestHelpers.Instance.LogLanguageLocalizer,
                new SessionRefHolder(),
                new PacketRateLimitService(TestHelpers.Instance.WorldConfiguration, TestHelpers.Instance.Clock));
            _keepAlive = 1;
            _session.LastKeepAliveIdentity = _keepAlive;
        }

        [TestMethod]
        public async Task ABurstWithinTheLimitIsHandled()
        {
            await new Spec("Sixty packets at once fit in the burst and are all handled")
                .WhenAsync(Sending_TargetInfoRequestsAtOnce, 60)
                .Then(_PacketsOf_AreHandled, 60, typeof(NcifPacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketsBeyondTheBurstAreDropped()
        {
            await new Spec("Packets past the burst within the same instant are dropped")
                .WhenAsync(Sending_TargetInfoRequestsAtOnce, 80)
                .Then(_PacketsOf_AreHandled, 60, typeof(NcifPacket))
                .And(TheSessionIsNotDisconnected)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketsAtASteadyRateAreAllHandled()
        {
            await new Spec("Two hundred packets at 25 a second are all handled")
                .WhenAsync(Sending_TargetInfoRequests_MillisecondsApart, 200, 40)
                .Then(_PacketsOf_AreHandled, 200, typeof(NcifPacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SustainedFloodingDisconnects()
        {
            await new Spec("A session that keeps sending past the limit is disconnected")
                .WhenAsync(Sending_TargetInfoRequestsAtOnce, 300)
                .Then(TheSessionIsDisconnected)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChatFasterThanTheChatLimitIsDropped()
        {
            await new Spec("A second chat line within half a second is dropped")
                .WhenAsync(Saying)
                .AndAsync(Saying)
                .Then(_PacketsOf_AreHandled, 1, typeof(ClientSayPacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NormalChatIsHandled()
        {
            await new Spec("Chat lines a second apart are all handled")
                .WhenAsync(Saying)
                .And(_MillisecondsPass, 1000)
                .AndAsync(Saying)
                .And(_MillisecondsPass, 1000)
                .AndAsync(Saying)
                .Then(_PacketsOf_AreHandled, 3, typeof(ClientSayPacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EmoticonsFasterThanTheLimitAreDropped()
        {
            await new Spec("A second emoticon within half a second is dropped")
                .WhenAsync(ShowingAnEmoticon)
                .AndAsync(ShowingAnEmoticon)
                .Then(_PacketsOf_AreHandled, 1, typeof(GuriPacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SkillCastsFasterThanTheLimitAreDroppedAndReleaseTheClient()
        {
            await new Spec("Skill casts closer than the limit are dropped, while a normal rotation is handled")
                .WhenAsync(CastingASkill)
                .AndAsync(CastingASkill)
                .And(_MillisecondsPass, 500)
                .AndAsync(CastingASkill)
                .Then(_PacketsOf_AreHandled, 2, typeof(UseSkillPacket))
                .And(TheDroppedCastIsCancelled)
                .ExecuteAsync();
        }

        private Task SendAsync(IPacket packet)
        {
            packet.KeepAliveId = (ushort)++_keepAlive;
            packet.Header = _session.GetPacketAttribute(packet.GetType())!.Identification;
            return _strategy.HandlePacketAsync(packet, _session, true);
        }

        private async Task Sending_TargetInfoRequestsAtOnce(int count)
        {
            for (var i = 0; i < count; i++)
            {
                await SendAsync(new NcifPacket { Type = VisualType.Monster, TargetId = 1 });
            }
        }

        private async Task Sending_TargetInfoRequests_MillisecondsApart(int count, int ms)
        {
            for (var i = 0; i < count; i++)
            {
                await SendAsync(new NcifPacket { Type = VisualType.Monster, TargetId = 1 });
                TestHelpers.Instance.Clock.Advance(Duration.FromMilliseconds(ms));
            }
        }

        private Task Saying() => SendAsync(new ClientSayPacket { Message = "hello" });

        private Task ShowingAnEmoticon() => SendAsync(new GuriPacket
        {
            Type = GuriPacketType.TextInput, Argument = 0, VisualId = _session.Character.VisualId, Data = 973
        });

        private Task CastingASkill() => SendAsync(new UseSkillPacket
        {
            CastId = 0, TargetVisualType = VisualType.Monster, TargetId = 1
        });

        private void _MillisecondsPass(int ms) => TestHelpers.Instance.Clock.Advance(Duration.FromMilliseconds(ms));

        private void _PacketsOf_AreHandled(int count, Type packetType) =>
            Assert.AreEqual(count, _handled.GetValueOrDefault(packetType));

        private void TheDroppedCastIsCancelled() =>
            Assert.AreEqual(1, _session.LastPackets.OfType<CancelPacket>().Count());

        private void TheSessionIsDisconnected() =>
            _logger.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);

        private void TheSessionIsNotDisconnected() =>
            _logger.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);

        private sealed class Counting<TPacket>(Dictionary<Type, int> handled) : PacketHandler<TPacket>, IWorldPacketHandler
            where TPacket : IPacket
        {
            public override Task ExecuteAsync(TPacket packet, ClientSession clientSession)
            {
                handled[typeof(TPacket)] = handled.GetValueOrDefault(typeof(TPacket)) + 1;
                return Task.CompletedTask;
            }
        }
    }
}
