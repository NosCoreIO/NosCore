//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using TwoFactorAuthNet;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class MfaHandlerTests
    {
        private ClientSession _session = null!;
        private MfaHandler _handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _handler = new MfaHandler();
        }

        [TestMethod]
        public async Task WrongTypeIsIgnored()
        {
            await new Spec("Guri type other than TextInput is ignored — no re-prompt, no IncorrectPassword msg")
                .Given(AccountHasMfaSecret)
                .WhenAsync(HandlingGuri_, GuriPacketType.AfterSumming, 3, 0L, "123456")
                .Then(NoIncorrectPasswordShouldBeSent)
                .And(NoMfaRePromptShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WrongArgumentIsIgnored()
        {
            await new Spec("Argument must be 3 (MFA field) — any other arg is ignored")
                .Given(AccountHasMfaSecret)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 4, 0L, "123456")
                .Then(NoIncorrectPasswordShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NonZeroVisualIdIsIgnored()
        {
            await new Spec("VisualId must be 0 (server dialog owner) — non-zero is ignored")
                .Given(AccountHasMfaSecret)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 3, 42L, "123456")
                .Then(NoIncorrectPasswordShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AlreadyValidatedSessionIsIgnored()
        {
            await new Spec("When session.MfaValidated is already true, re-entering a code is a no-op")
                .Given(AccountHasMfaSecret)
                .And(SessionIsAlreadyValidated)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 3, 0L, "wrong!")
                .Then(NoIncorrectPasswordShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AccountWithoutMfaSecretIsIgnored()
        {
            await new Spec("Accounts that never set up MFA are skipped — no re-prompt, no error")
                .Given(AccountHasNoMfaSecret)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 3, 0L, "123456")
                .Then(NoIncorrectPasswordShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task IncorrectCodeRePromptsAndSendsIncorrectPassword()
        {
            await new Spec("An incorrect code re-sends the TextInput prompt and an IncorrectPassword info message")
                .Given(AccountHasMfaSecret)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 3, 0L, "000000")
                .Then(MfaRePromptShouldBeSent)
                .And(IncorrectPasswordShouldBeSent)
                .And(SessionShouldStillBeUnvalidated)
                .ExecuteAsync();
        }

        private void AccountHasMfaSecret() =>
            _session.Account.MfaSecret = new TwoFactorAuth().CreateSecret();

        private void AccountHasNoMfaSecret() =>
            _session.Account.MfaSecret = null;

        private void SessionIsAlreadyValidated() =>
            _session.MfaValidated = true;

        private async Task HandlingGuri_(GuriPacketType type, int argument, long visualId, string value)
        {
            var packet = new GuriPacket
            {
                Type = type,
                Argument = argument,
                VisualId = visualId,
                Value = value,
            };
            await _handler.Handle(new GuriPacketReceivedEvent(_session, packet));
        }

        private void NoIncorrectPasswordShouldBeSent() =>
            Assert.IsFalse(_session.LastPackets.OfType<InfoiPacket>()
                .Any(p => p.Message == Game18NConstString.IncorrectPassword));

        private void NoMfaRePromptShouldBeSent() =>
            Assert.IsFalse(_session.LastPackets
                .OfType<NosCore.Packets.ServerPackets.UI.GuriPacket>()
                .Any(p => p.Type == GuriPacketType.TextInput && p.Argument == 3));

        private void IncorrectPasswordShouldBeSent()
        {
            var info = _session.LastPackets.OfType<InfoiPacket>()
                .FirstOrDefault(p => p.Message == Game18NConstString.IncorrectPassword);
            Assert.IsNotNull(info);
        }

        private void MfaRePromptShouldBeSent()
        {
            var guri = _session.LastPackets.OfType<NosCore.Packets.ServerPackets.UI.GuriPacket>()
                .FirstOrDefault(p => p.Type == GuriPacketType.TextInput && p.Argument == 3);
            Assert.IsNotNull(guri);
        }

        private void SessionShouldStillBeUnvalidated() =>
            Assert.IsFalse(_session.MfaValidated);
    }
}
