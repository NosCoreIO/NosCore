//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwoFactorAuthNet;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Networking
{
    [TestClass]
    public class WorldPacketHandlingStrategyTests
    {
        private ClientSession Session = null!;
        private readonly List<string> Handled = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Handled.Clear();
            Session = await TestHelpers.Instance.GenerateSessionAsync(new List<IPacketHandler>
            {
                new RecordingHandler<SetMaintenancePacket>(Handled),
                new RecordingHandler<SelectPacket>(Handled),
                new RecordingHandler<ClientGuriPacket>(Handled),
            });
            Session.LastKeepAliveIdentity = 1;
        }

        [TestMethod]
        public async Task GameMasterCommandRunsForAGameMaster()
        {
            await new Spec("Game master command runs for a game master")
                .Given(AccountAuthorityIs_, AuthorityType.GameMaster)
                .WhenAsync(AGameMasterCommandArrives)
                .Then(Packet_ShouldHaveRun, nameof(SetMaintenancePacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameMasterCommandIsRefusedForAUser()
        {
            await new Spec("Game master command is refused for a user")
                .Given(AccountAuthorityIs_, AuthorityType.User)
                .WhenAsync(AGameMasterCommandArrives)
                .Then(Packet_ShouldNotHaveRun, nameof(SetMaintenancePacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameMasterCommandIsRefusedBelowUserAuthority()
        {
            await new Spec("Game master command is refused below user authority")
                .Given(AccountAuthorityIs_, AuthorityType.Banned)
                .WhenAsync(AGameMasterCommandArrives)
                .Then(Packet_ShouldNotHaveRun, nameof(SetMaintenancePacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CharacterScreenPacketIsRefusedWhileMfaIsPending()
        {
            await new Spec("Character screen packet is refused while MFA is pending")
                .Given(TheSessionIsOnTheCharacterScreen)
                .And(AccountHasMfaAndItIsNotValidated)
                .WhenAsync(ASelectArrives)
                .Then(Packet_ShouldNotHaveRun, nameof(SelectPacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CharacterScreenPacketRunsOnceMfaIsValidated()
        {
            await new Spec("Character screen packet runs once MFA is validated")
                .Given(TheSessionIsOnTheCharacterScreen)
                .And(AccountHasMfaAndItIsNotValidated)
                .And(MfaIsValidated)
                .WhenAsync(ASelectArrives)
                .Then(Packet_ShouldHaveRun, nameof(SelectPacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MfaCodeIsAcceptedWhileMfaIsPending()
        {
            await new Spec("MFA code is accepted while MFA is pending")
                .Given(TheSessionIsOnTheCharacterScreen)
                .And(AccountHasMfaAndItIsNotValidated)
                .WhenAsync(AnMfaCodeArrives)
                .Then(Packet_ShouldHaveRun, typeof(ClientGuriPacket).Name)
                .ExecuteAsync();
        }

        private void AccountAuthorityIs_(AuthorityType authority) => Session.Account.Authority = authority;

        private void TheSessionIsOnTheCharacterScreen() => Session.ClearPlayerEntity();

        private void AccountHasMfaAndItIsNotValidated()
        {
            Session.Account.MfaSecret = new TwoFactorAuth().CreateSecret();
            Session.MfaValidated = false;
        }

        private void MfaIsValidated() => Session.MfaValidated = true;

        private Task AGameMasterCommandArrives() =>
            FromNetwork(new SetMaintenancePacket { Header = "$SetMaintenance" });

        private Task ASelectArrives() => FromNetwork(new SelectPacket { Header = "select", Slot = 1 });

        private Task AnMfaCodeArrives() => FromNetwork(new ClientGuriPacket
        {
            Header = "guri",
            Type = GuriPacketType.TextInput,
            Argument = 3,
            VisualId = 0,
            Value = "123456",
        });

        private Task FromNetwork(IPacket packet)
        {
            packet.KeepAliveId = (ushort)(Session.LastKeepAliveIdentity + 1);
            return Session.HandlePacketsAsync(new[] { packet }, true);
        }

        private void Packet_ShouldHaveRun(string name) => CollectionAssert.Contains(Handled, name);

        private void Packet_ShouldNotHaveRun(string name) => CollectionAssert.DoesNotContain(Handled, name);

        private sealed class RecordingHandler<TPacket>(List<string> handled) : PacketHandler<TPacket>, IWorldPacketHandler
            where TPacket : IPacket
        {
            public override Task ExecuteAsync(TPacket packet, ClientSession clientSession)
            {
                handled.Add(typeof(TPacket).Name);
                return Task.CompletedTask;
            }
        }
    }
}
