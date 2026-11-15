//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class ManaShieldTests
    {
        private TestWorld World;
        private IAliveEntity Target;
        private List<BuffInstance> Buffs;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Target = (await World.CreateSessionAsync()).Character;
            Buffs = new List<BuffInstance>();
            World.Freeze<Mock<IBuffService>>().Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(Buffs);
            World.Freeze<Mock<IEquipmentStatsService>>().Setup(g => g.Resolve(It.IsAny<IAliveEntity>()))
                .Returns(EquipmentStats.None);
        }

        private void Shielded(int percent) =>
            Buffs.Add(new BuffInstance(88, BuffType.Good, null, Instant.MinValue, Instant.MaxValue,
                new[]
                {
                    new BCardDto
                    {
                        Type = BCardEffect.LightAndShadowInflictDamageToMp.Type(),
                        SubType = BCardEffect.LightAndShadowInflictDamageToMp.SubType(),
                        FirstData = percent
                    }
                }));

        private Task<int> Hit(int damage) => World.Create<ManaShieldService>().AbsorbAsync(Target, damage);

        [TestMethod]
        public async Task ManaShieldPaysItsShareFromMp()
        {
            Target.Mp = 1000;
            Shielded(25);

            Assert.AreEqual(300, await Hit(400));
            Assert.AreEqual(900, Target.Mp);
        }

        [TestMethod]
        public async Task OnlyTheMpLeftCanPay()
        {
            Target.Mp = 40;
            Shielded(25);

            Assert.AreEqual(360, await Hit(400));
            Assert.AreEqual(0, Target.Mp);
        }

        [TestMethod]
        public async Task WithoutAShieldTheWholeBlowGoesToHp()
        {
            Target.Mp = 1000;

            Assert.AreEqual(400, await Hit(400));
            Assert.AreEqual(1000, Target.Mp);
        }
    }
}
