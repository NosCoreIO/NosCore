//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // BCard 22/32, "%s HP lost while casting": Deadly Poison (card 60) takes 250 HP per cast,
    // Internal Injuries (card 695) 5 HP per level of whoever inflicted it.
    [TestClass]
    public class CastingHpLossTests
    {
        private static readonly CardDto Injuries = new() { CardId = 695, Duration = 50 };

        private TestWorld World;
        private ClientSession Session;
        private BuffService Buffs;
        private int HpBefore;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.Mp = 1000;
            Buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(Buffs);
        }

        [TestMethod]
        public async Task EachCastCostsTheValueTimesTheInflictersLevel()
        {
            await new Spec("Under Internal Injuries inflicted by a level 10 monster, each cast costs 50 HP")
                .Given(TheCharacterHasFullHp)
                .AndAsync(ALevel_MonsterInflicts_, (byte)10, LossWhileCasting(5, levelScaled: true))
                .WhenAsync(CastingASkillAsync)
                .Then(TheCharacterShouldHaveLost_Hp, 50)
                .And(TheMapShouldSeeTheCharacterLose_, 50)
                .And(TheCharacterShouldHaveBeenSentItsStat)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EveryCastCostsItAgain()
        {
            await new Spec("A loss that does not scale is taken as it is, at every cast")
                .Given(TheCharacterHasFullHp)
                .AndAsync(ALevel_MonsterInflicts_, (byte)30, LossWhileCasting(40))
                .WhenAsync(CastingASkillAsync)
                .AndAsync(CastingASkillAsync)
                .Then(TheCharacterShouldHaveLost_Hp, 80)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACastNeverTakesTheLastHp()
        {
            await new Spec("Under Deadly Poison a character with less HP than its 250 is left with 1 HP")
                .Given(TheCharacterHas_Hp, 100)
                .AndAsync(ALevel_MonsterInflicts_, (byte)30, LossWhileCasting(250))
                .WhenAsync(CastingASkillAsync)
                .Then(TheCharacterShouldHave_Hp, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutTheDebuffACastCostsNoHp()
        {
            await new Spec("Without the debuff a cast costs mana only")
                .Given(TheCharacterHasFullHp)
                .WhenAsync(CastingASkillAsync)
                .Then(TheCharacterShouldHaveLost_Hp, 0)
                .ExecuteAsync();
        }

        private static BCardDto LossWhileCasting(int value, bool levelScaled = false) => new()
        {
            Type = BCardEffect.HealingBurningAndCastingDecreaseHpWhenCasting.Type(),
            SubType = BCardEffect.HealingBurningAndCastingDecreaseHpWhenCasting.SubType(),
            FirstData = value, IsLevelScaled = levelScaled,
        };

        private void TheCharacterHasFullHp() => TheCharacterHas_Hp(Session.Character.MaxHp);

        private void TheCharacterHas_Hp(int hp)
        {
            Session.Character.Hp = hp;
            HpBefore = hp;
        }

        private Task ALevel_MonsterInflicts_(byte level, BCardDto bCard)
        {
            var map = Session.Character.MapInstance;
            var creature = new NpcMonsterDto { NpcMonsterVNum = 333, Level = level, MaxHp = 1000, MaxMp = 10 };
            var monster = new MonsterComponentBundle(
                map.EcsWorld.CreateMonster(9001, creature, map, 12, 14, 2, 12, 14, false, true, false), map.EcsWorld);
            return Buffs.ApplyAsync(Session.Character, Injuries, [bCard], monster);
        }

        private Task CastingASkillAsync()
        {
            Session.LastPackets.Clear();
            Session.Character.MapInstance.LastPackets.Clear();
            var skill = new CharacterSkill
            {
                CharacterId = Session.Character.CharacterId,
                SkillVNum = 1,
                Skill = new SkillDto { SkillVNum = 1, CastId = 1, MpCost = 10 },
            };
            return World.Create<SkillCastService>().SpendAsync(Session.Character, skill, 10);
        }

        private void TheCharacterShouldHaveLost_Hp(int hp) => Assert.AreEqual(HpBefore - hp, Session.Character.Hp);

        private void TheCharacterShouldHave_Hp(int hp) => Assert.AreEqual(hp, Session.Character.Hp);

        private void TheMapShouldSeeTheCharacterLose_(int hp) =>
            PacketAssert.AreEqual(new DmPacket
            {
                VisualType = VisualType.Player, VisualId = Session.Character.VisualId, Damages = hp,
                DamageType = DamagePacketType.Damage,
            }, Session.Character.MapInstance.LastPackets.OfType<DmPacket>().Single());

        private void TheCharacterShouldHaveBeenSentItsStat() =>
            Assert.AreEqual(Session.Character.Hp, Session.LastPackets.OfType<StatPacket>().Last().Hp);
    }
}
