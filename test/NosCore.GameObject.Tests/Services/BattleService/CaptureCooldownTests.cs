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
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using FakeEntity = NosCore.GameObject.Tests.Services.BattleService.BattleServiceTests.FakeEntity;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class CaptureCooldownTests
    {
        private const long CastId = 16;

        private static readonly SkillInfo Capture = new(209, CastId, 150, 15, -1, 0, 0, TargetHitType.SingleTargetHit,
            1, 0, 0, 0, 0, 10, new List<BCardDto>
            {
                new() { Type = BCardEffect.CaptureCaptureAnimal.Type(), SubType = BCardEffect.CaptureCaptureAnimal.SubType() }
            });

        private TestWorld World;
        private ClientSession _caster;
        private Mock<ICaptureService> _captures;
        private FakeEntity _monster;
        private bool _accepted;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _caster = await World.CreateSessionAsync();
            _caster.Character.MapInstance = World.GetBaseMap(TestWorld.TestMapId);
            _monster = new FakeEntity { Hp = 100, VisualType = VisualType.Monster, VisualId = 7, MapInstance = _caster.Character.MapInstance };
            World.Freeze<Mock<ISkillResolver>>()
                .Setup(r => r.Resolve(It.IsAny<IAliveEntity>(), CastId)).Returns(Capture);
            _captures = World.Freeze<Mock<ICaptureService>>();
            _captures.Setup(c => c.IsCaptureSkill(Capture)).Returns(true);
            World.UseReal<ISkillCastService, SkillCastService>();
        }

        [TestMethod]
        public async Task ARefusedAttemptStartsTheClientsCooldown()
        {
            await new Spec("A capture refused for the monster's health still starts the skill's cooldown on the client")
                .Given(TheAttempt_, true)
                .WhenAsync(TheCasterCaptures)
                .Then(TheCastShouldBeAccepted_, true)
                .And(TheCooldownShouldStartFor_Tenths, (short)150)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SomethingThatCannotBeCapturedCostsNothing()
        {
            await new Spec("A capture at something that cannot be captured is refused before mana or cooldown is spent")
                .Given(TheAttempt_, false)
                .WhenAsync(TheCasterCaptures)
                .Then(TheCastShouldBeAccepted_, false)
                .And(NoCooldownShouldStart)
                .ExecuteAsync();
        }

        private void TheAttempt_(bool counts) =>
            _captures.Setup(c => c.TryCaptureAsync(It.IsAny<IAliveEntity>(), _monster, Capture)).ReturnsAsync(counts);

        private async Task TheCasterCaptures() =>
            _accepted = await World.Create<GameObject.Services.BattleService.BattleService>()
                .Hit(_caster.Character, _monster, new HitArguments { SkillId = CastId });

        private void TheCastShouldBeAccepted_(bool accepted) => Assert.AreEqual(accepted, _accepted);

        private void TheCooldownShouldStartFor_Tenths(short tenths) =>
            PacketAssert.AreEqual(new SkillResetPacket { CastId = -10, Unknown1 = (short)CastId, Unknown2 = tenths },
                _caster.LastPackets.OfType<SkillResetPacket>().Single());

        private void NoCooldownShouldStart()
        {
            Assert.IsFalse(_caster.LastPackets.OfType<SkillResetPacket>().Any());
            Assert.IsFalse(_caster.Character.PendingResets.ContainsKey(CastId));
        }
    }
}
