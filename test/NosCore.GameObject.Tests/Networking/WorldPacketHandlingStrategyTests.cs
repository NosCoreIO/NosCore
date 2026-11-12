//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Bank;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwoFactorAuthNet;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Networking
{
    [TestClass]
    public class WorldPacketHandlingStrategyTests
    {
        private TestWorld World;
        private ClientSession Session;
        private readonly List<string> Handled = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Handled.Clear();
            Session = await World.CreateSessionAsync(new List<IPacketHandler>
            {
                new RecordingHandler<SetMaintenancePacket>(Handled),
                new RecordingHandler<SelectPacket>(Handled),
                new RecordingHandler<ClientGuriPacket>(Handled),
                new RecordingHandler<GboxPacket>(Handled),
                new RecordingHandler<RemovePacket>(Handled),
                new RecordingHandler<SortOpenPacket>(Handled),
                new RecordingHandler<ISortPacket>(Handled),
                new RecordingHandler<BiPacket>(Handled),
                new RecordingHandler<UseItemPacket>(Handled),
                new RecordingHandler<WearPacket>(Handled),
                new RecordingHandler<NrunPacket>(Handled),
                new RecordingHandler<UseSkillPacket>(Handled),
                new RecordingHandler<SCarrierPacket>(Handled),
                new RecordingHandler<RevivalPacket>(Handled),
                new RecordingHandler<ClientSayPacket>(Handled),
                new RecordingHandler<PulsePacket>(Handled),
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

        [DataTestMethod]
        [DataRow(nameof(GboxPacket))]
        [DataRow(nameof(RemovePacket))]
        [DataRow(nameof(SortOpenPacket))]
        [DataRow(nameof(ISortPacket))]
        [DataRow(nameof(BiPacket))]
        [DataRow(nameof(UseItemPacket))]
        [DataRow(nameof(WearPacket))]
        [DataRow(nameof(NrunPacket))]
        public async Task PacketOutsideTheTradeScopeIsRefusedDuringAnExchange(string packet)
        {
            await new Spec("A packet the trade scope does not admit is refused during an exchange")
                .Given(TheCharacterIsInAnExchange)
                .WhenAsync(A_Arrives, packet)
                .Then(Packet_ShouldNotHaveRun, packet)
                .ExecuteAsync();
        }

        [DataTestMethod]
        [DataRow(nameof(GboxPacket))]
        [DataRow(nameof(RemovePacket))]
        [DataRow(nameof(SortOpenPacket))]
        [DataRow(nameof(ISortPacket))]
        [DataRow(nameof(BiPacket))]
        [DataRow(nameof(UseItemPacket))]
        [DataRow(nameof(WearPacket))]
        [DataRow(nameof(NrunPacket))]
        public async Task PacketOutsideTheTradeScopeIsRefusedInAShop(string packet)
        {
            await new Spec("A packet the trade scope does not admit is refused in a shop")
                .Given(TheCharacterIsInAShop)
                .WhenAsync(A_Arrives, packet)
                .Then(Packet_ShouldNotHaveRun, packet)
                .ExecuteAsync();
        }

        [DataTestMethod]
        [DataRow(nameof(GboxPacket))]
        [DataRow(nameof(RemovePacket))]
        [DataRow(nameof(SortOpenPacket))]
        [DataRow(nameof(ISortPacket))]
        [DataRow(nameof(BiPacket))]
        [DataRow(nameof(UseItemPacket))]
        [DataRow(nameof(WearPacket))]
        [DataRow(nameof(NrunPacket))]
        [DataRow(nameof(UseSkillPacket))]
        [DataRow(nameof(SCarrierPacket))]
        public async Task PacketRunsForAnAliveCharacterOutsideAnyTrade(string packet)
        {
            await new Spec("A packet runs for an alive character who is not trading, mounted or transformed")
                .WhenAsync(A_Arrives, packet)
                .Then(Packet_ShouldHaveRun, packet)
                .ExecuteAsync();
        }

        [DataTestMethod]
        [DataRow(nameof(UseItemPacket))]
        [DataRow(nameof(GboxPacket))]
        [DataRow(nameof(UseSkillPacket))]
        public async Task PacketNotAdmittedWhileDeadIsRefusedWhileDead(string packet)
        {
            await new Spec("A packet without the while-dead scope is refused while dead")
                .Given(TheCharacterIsDead)
                .WhenAsync(A_Arrives, packet)
                .Then(Packet_ShouldNotHaveRun, packet)
                .ExecuteAsync();
        }

        [DataTestMethod]
        [DataRow(nameof(ClientSayPacket))]
        [DataRow(nameof(PulsePacket))]
        [DataRow(nameof(RevivalPacket))]
        public async Task PacketAdmittedWhileDeadRunsWhileDead(string packet)
        {
            await new Spec("A packet with the while-dead scope runs while dead")
                .Given(TheCharacterIsDead)
                .WhenAsync(A_Arrives, packet)
                .Then(Packet_ShouldHaveRun, packet)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GameMasterCommandRunsWhileDead()
        {
            await new Spec("Game master command runs while dead")
                .Given(AccountAuthorityIs_, AuthorityType.GameMaster)
                .And(TheCharacterIsDead)
                .WhenAsync(AGameMasterCommandArrives)
                .Then(Packet_ShouldHaveRun, nameof(SetMaintenancePacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketDispatchedByTheServerIsRefusedWhileDead()
        {
            await new Spec("A packet the server dispatches itself is refused while dead too")
                .Given(TheCharacterIsDead)
                .WhenAsync(TheServerDispatches_, nameof(UseItemPacket))
                .Then(Packet_ShouldNotHaveRun, nameof(UseItemPacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketAfterTheCounterWrapsRuns()
        {
            await new Spec("The client follows 65534 with 0, so that packet runs")
                .Given(TheLastPacketWasNumbered_, (ushort)(ushort.MaxValue - 1))
                .WhenAsync(A_Arrives, nameof(PulsePacket))
                .Then(Packet_ShouldHaveRun, nameof(PulsePacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketNumbered65535IsRefused()
        {
            await new Spec("The client never sends 65535, so a packet numbered 65535 is refused")
                .Given(TheLastPacketWasNumbered_, (ushort)(ushort.MaxValue - 1))
                .WhenAsync(APulseNumbered_, ushort.MaxValue)
                .Then(Packet_ShouldNotHaveRun, nameof(PulsePacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketThatSkipsANumberIsRefused()
        {
            await new Spec("A packet that skips a number is refused")
                .WhenAsync(APulseNumbered_, (ushort)3)
                .Then(Packet_ShouldNotHaveRun, nameof(PulsePacket))
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RevivalIsRefusedWhileAlive()
        {
            await new Spec("Revival is refused while alive, without a message")
                .WhenAsync(A_Arrives, nameof(RevivalPacket))
                .Then(Packet_ShouldNotHaveRun, nameof(RevivalPacket))
                .And(NoMessageShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SkillIsRefusedWhileVehicled()
        {
            await new Spec("A skill is refused on a vehicle, without a message")
                .Given(TheCharacterIsVehicled)
                .WhenAsync(A_Arrives, nameof(UseSkillPacket))
                .Then(Packet_ShouldNotHaveRun, nameof(UseSkillPacket))
                .And(NoMessageShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpecialistHolderIsRefusedWhileTransformed()
        {
            await new Spec("Storing a specialist is refused while transformed, with the client's message")
                .Given(TheCharacterIsTransformed)
                .WhenAsync(A_Arrives, nameof(SCarrierPacket))
                .Then(Packet_ShouldNotHaveRun, nameof(SCarrierPacket))
                .And(MessageShouldBe_, Game18NConstString.CantPutSpecialistInHolderWhileTransformed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PacketWithoutARefusalRunsWhileTransformedOrVehicled()
        {
            await new Spec("A packet that declares no refusal runs while transformed and vehicled")
                .Given(TheCharacterIsTransformed)
                .And(TheCharacterIsVehicled)
                .WhenAsync(A_Arrives, nameof(UseItemPacket))
                .Then(Packet_ShouldHaveRun, nameof(UseItemPacket))
                .ExecuteAsync();
        }

        private void TheCharacterIsInAnExchange() => Session.Character.InExchange = true;

        private void TheCharacterIsInAShop() => Session.Character.InShop = true;

        private void TheCharacterIsDead() => Session.Character.IsAlive = false;

        private void TheCharacterIsVehicled() => Session.Character.IsVehicled = true;

        private void TheCharacterIsTransformed() => Session.Character.UseSp = true;

        private Task A_Arrives(string packet) => FromNetwork(PacketNamed(packet));

        private void TheLastPacketWasNumbered_(ushort id) => Session.LastKeepAliveIdentity = id;

        private Task APulseNumbered_(ushort id)
        {
            var packet = PacketNamed(nameof(PulsePacket));
            packet.KeepAliveId = id;
            return Session.HandlePacketsAsync(new[] { packet }, true);
        }

        private Task TheServerDispatches_(string packet) => Session.HandlePacketsAsync(new[] { PacketNamed(packet) });

        private static IPacket PacketNamed(string name) => name switch
        {
            nameof(GboxPacket) => new GboxPacket { Header = "gbox", Type = BankActionType.DepositGold, Amount = 1 },
            nameof(RemovePacket) => new RemovePacket { Header = "remove", InventorySlot = EquipmentType.MainWeapon },
            nameof(SortOpenPacket) => new SortOpenPacket { Header = "sortopen" },
            nameof(ISortPacket) => new ISortPacket { Header = "isort", PocketType = PocketType.Main },
            nameof(BiPacket) => new BiPacket { Header = "b_i", PocketType = PocketType.Main, Option = RequestDeletionType.Confirmed },
            nameof(UseItemPacket) => new UseItemPacket { Header = "u_i", Type = PocketType.Main },
            nameof(WearPacket) => new WearPacket { Header = "wear" },
            nameof(NrunPacket) => new NrunPacket { Header = "n_run", Runner = NrunRunnerType.OpenBankFacilities },
            nameof(UseSkillPacket) => new UseSkillPacket { Header = "u_s", TargetVisualType = VisualType.Monster },
            nameof(SCarrierPacket) => new SCarrierPacket { Header = "s_carrier" },
            nameof(RevivalPacket) => new RevivalPacket { Header = "revival", Type = RevivalType.AtSavePoint },
            nameof(ClientSayPacket) => new ClientSayPacket { Header = "say", Message = "hello" },
            nameof(PulsePacket) => new PulsePacket { Header = "pulse", Tick = 60 },
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
        };

        private void NoMessageShouldBeSent() => Assert.IsFalse(Session.LastPackets.OfType<MsgiPacket>().Any());

        private void MessageShouldBe_(Game18NConstString message) =>
            Assert.AreEqual(message, Session.LastPackets.OfType<MsgiPacket>().Single().Message);

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
            packet.KeepAliveId = Session.LastKeepAliveIdentity == ushort.MaxValue - 1
                ? (ushort)0
                : (ushort)((Session.LastKeepAliveIdentity ?? 0) + 1);
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
