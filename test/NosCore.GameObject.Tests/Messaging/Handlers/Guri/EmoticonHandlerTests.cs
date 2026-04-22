//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class EmoticonHandlerTests
    {
        private ClientSession _session = null!;
        private EmoticonHandler _handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _handler = new EmoticonHandler();
        }

        [TestMethod]
        public async Task DataBelowRangeIsIgnored()
        {
            await new Spec("Data < 973 is out of the emoticon range and ignored")
                .Given(EmoticonsAreAllowed)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 972, _Owner())
                .Then(NoEffPacketShouldHaveBeenBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DataAboveRangeIsIgnored()
        {
            await new Spec("Data > 999 is out of the emoticon range and ignored")
                .Given(EmoticonsAreAllowed)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 1000, _Owner())
                .Then(NoEffPacketShouldHaveBeenBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WrongGuriTypeIsIgnored()
        {
            await new Spec("Guri type other than TextInput is ignored by the emoticon handler")
                .Given(EmoticonsAreAllowed)
                .WhenAsync(HandlingGuri_, GuriPacketType.AfterSumming, 973, _Owner())
                .Then(NoEffPacketShouldHaveBeenBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EmoticonsBlockedSuppressesBroadcast()
        {
            await new Spec("Character with EmoticonsBlocked flag does not broadcast the effect")
                .Given(EmoticonsAreBlocked)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 975, _Owner())
                .Then(NoEffPacketShouldHaveBeenBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpoofedVisualIdIsIgnored()
        {
            await new Spec("Packet with a VisualId not matching the caller's character is anti-spoof ignored")
                .Given(EmoticonsAreAllowed)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 975, 999999L)
                .Then(NoEffPacketShouldHaveBeenBroadcast)
                .ExecuteAsync();
        }

        private long _Owner() => _session.Character.CharacterId;

        private void EmoticonsAreAllowed() => _session.Character.EmoticonsBlocked = false;
        private void EmoticonsAreBlocked() => _session.Character.EmoticonsBlocked = true;

        private async Task HandlingGuri_(GuriPacketType type, int data, long visualId)
        {
            var packet = new GuriPacket
            {
                Type = type,
                Data = data,
                VisualId = visualId,
            };
            await _handler.Handle(new GuriPacketReceivedEvent(_session, packet));
        }

        private void NoEffPacketShouldHaveBeenBroadcast() =>
            Assert.IsFalse(_session.LastPackets.OfType<EffectPacket>().Any(),
                "Expected no EffectPacket broadcast");
    }
}
