//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    // The two percentages in `st` are the health and mana bars the client draws.
    [TestClass]
    public class StatInfoPercentageTests
    {
        private TestWorld World;
        private ClientSession _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public void WithARealMaximumThePercentagesAreTheOrdinaryOnes()
        {
            _session.Character.MaxHp = 1000;
            _session.Character.Hp = 250;
            _session.Character.MaxMp = 400;
            _session.Character.Mp = 400;

            var packet = _session.Character.GenerateStatInfo();

            Assert.AreEqual(25, packet.HpPercentage);
            Assert.AreEqual(100, packet.MpPercentage);
        }

        [TestMethod]
        public void AZeroMaximumShowsAnEmptyBar()
        {
            _session.Character.MaxHp = 0;
            _session.Character.Hp = 0;
            _session.Character.MaxMp = 0;
            _session.Character.Mp = 0;

            var packet = _session.Character.GenerateStatInfo();

            Assert.AreEqual(0, packet.HpPercentage);
            Assert.AreEqual(0, packet.MpPercentage);
        }

        [TestMethod]
        public void APercentageRoundsDown()
        {
            _session.Character.MaxHp = 777;
            _session.Character.Hp = 111;

            var packet = _session.Character.GenerateStatInfo();

            Assert.AreEqual((int)(111 / 777f * 100), packet.HpPercentage);
        }

        [TestMethod]
        public void TheBuffListCarriesTheTimedBuffsButNotThePermanentOnes()
        {
            Buff(116, isStatic: false);
            Buff(131, isStatic: true);

            var packet = _session.Character.GenerateStatInfo();

            CollectionAssert.AreEqual(new short[] { 116 }, packet.BuffIds);
        }

        [TestMethod]
        public void WithoutABuffTheListIsLeftOut()
        {
            Assert.IsNull(_session.Character.GenerateStatInfo().BuffIds);
        }

        private void Buff(short cardId, bool isStatic) =>
            _session.Character.BuffState().NotNull()[cardId] = new BuffInstance(cardId, BuffType.Good, null,
                Instant.MinValue, Instant.MaxValue, Array.Empty<BCardDto>(), Static: isStatic);
    }
}
