//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Systems
{
    [TestClass]
    public class RegenerationSystemTests
    {
        private TestWorld World;
        private ClientSession _session;
        private readonly RegenerationSystem _system = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.Hp = 1;
        }

        [TestMethod]
        public void ASittingPlayerIsDueSoonerThanAStandingOne()
        {
            _session.Character.IsSitting = true;
            FindDue();

            World.Clock.Advance(Duration.FromMilliseconds(1500));
            Assert.AreEqual(1, FindDue().Count);

            _session.Character.IsSitting = false;
            World.Clock.Advance(Duration.FromMilliseconds(1500));
            Assert.AreEqual(0, FindDue().Count);
        }

        [TestMethod]
        public void PlayersWithFullBarsOrDeadAreNeverDue()
        {
            _session.Character.IsSitting = true;
            _session.Character.Hp = _session.Character.MaxHp;
            _session.Character.Mp = _session.Character.MaxMp;
            FindDue();
            World.Clock.Advance(Duration.FromSeconds(10));
            Assert.AreEqual(0, FindDue().Count);

            _session.Character.Hp = 1;
            _session.Character.IsAlive = false;
            World.Clock.Advance(Duration.FromSeconds(10));
            Assert.AreEqual(0, FindDue().Count);
        }

        private List<Entity> FindDue()
        {
            var due = new List<Entity>();
            _system.FindDue(_session.Character.MapInstance.EcsWorld, World.Clock.GetCurrentInstant(), due);
            return due;
        }
    }
}
