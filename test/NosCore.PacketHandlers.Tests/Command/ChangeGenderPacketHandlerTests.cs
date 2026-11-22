//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class ChangeGenderPacketHandlerTests
    {
        private TestWorld World;
        private ChangeGenderPacketHandler Handler;
        private ClientSession Session;
        private Mock<ISessionGroup> Onlookers;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.MapInstance = World.GetBaseMap(1);
            Onlookers = new Mock<ISessionGroup>();
            Session.Character.MapInstance.Sessions = Onlookers.Object;
            Handler = World.Create<ChangeGenderPacketHandler>();
        }

        [TestMethod]
        public async Task AMaleCharacterBecomesFemale()
        {
            await new Spec("A male character becomes female")
                .Given(TheCharacterIs_, GenderType.Male)
                .WhenAsync(ChangingGender)
                .Then(TheCharacterShouldBe_, GenderType.Female)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFemaleCharacterBecomesMale()
        {
            await new Spec("A female character becomes male")
                .Given(TheCharacterIs_, GenderType.Female)
                .WhenAsync(ChangingGender)
                .Then(TheCharacterShouldBe_, GenderType.Male)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheMapSeesTheChangeEffect()
        {
            await new Spec("The map sees the gender change effect")
                .Given(TheCharacterIs_, GenderType.Male)
                .WhenAsync(ChangingGender)
                .Then(TheMapShouldSeeTheEffect)
                .ExecuteAsync();
        }

        private void TheCharacterIs_(GenderType gender) => Session.Character.Gender = gender;

        private Task ChangingGender() => Handler.ExecuteAsync(new ChangeGenderPacket(), Session);

        private void TheCharacterShouldBe_(GenderType gender)
        {
            Assert.AreEqual(gender, Session.Character.Gender);
            PacketAssert.AreEqual(new PSexPacket { Gender = gender }, Session.LastPackets.OfType<PSexPacket>().Single());
        }

        private IEnumerable<IPacket> Broadcast() =>
            Onlookers.Invocations.SelectMany(i => i.Arguments)
                .SelectMany(a => a is IEnumerable<IPacket> many ? many : a is IPacket one ? new[] { one } : []);

        private void TheMapShouldSeeTheEffect() =>
            PacketAssert.AreEqual(new EffectPacket
            {
                EffectType = VisualType.Player,
                VisualEntityId = Session.Character.CharacterId,
                Id = 196,
            }, Broadcast().OfType<EffectPacket>().Single());
    }
}
