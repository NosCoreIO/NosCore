//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class SetHeroXpPacketHandlerTests
    {
        private TestWorld World;
        private SetHeroXpPacketHandler Handler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Handler = World.Create<SetHeroXpPacketHandler>();
        }

        [TestMethod]
        public async Task TheGivenExperienceIsSetAndShown()
        {
            await new Spec("The given hero experience is set and shown")
                .WhenAsync(SettingExperience_, 1500)
                .Then(ExperienceShouldBe_, 1500)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NegativeExperienceIsSetToZero()
        {
            await new Spec("Negative hero experience is set to zero")
                .Given(ExperienceIs_, 800)
                .WhenAsync(SettingExperience_, -20)
                .Then(ExperienceShouldBe_, 0)
                .ExecuteAsync();
        }

        private void ExperienceIs_(int value) => Session.Character.HeroLevelXp = value;

        private Task SettingExperience_(int value) =>
            Handler.ExecuteAsync(new SetHeroXpPacket { HeroXp = value }, Session);

        private void ExperienceShouldBe_(int value)
        {
            Assert.AreEqual((long)value, Session.Character.HeroLevelXp);
            Assert.AreEqual((long)value, Session.LastPackets.OfType<LevPacket>().Single().HeroXp);
        }
    }
}
