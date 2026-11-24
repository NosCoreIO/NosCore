//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Algorithm.RecoveryRateService;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // BCard 36/2 and 36/3: no HP or MP consumption on one side, no HP or MP recovery on the other.
    [TestClass]
    public class TimeCircleTests
    {
        private static readonly CardDto Shield = new() { CardId = 1, Duration = 600 };
        private static readonly CardDto Poison = new() { CardId = 2, Duration = 600 };

        private TestWorld World;
        private ClientSession Session;
        private BuffService Buffs;
        private int HpBefore;
        private int MpBefore;
        private int MpCost;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.Inject<IRecoveryRateService>(new RecoveryRateService());
            World.Freeze<Mock<IEquipmentStatsService>>()
                .Setup(e => e.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            Session = await World.CreateSessionAsync();
            Session.Character.Level = 50;
            Session.Character.MaxHp = 1000;
            Session.Character.MaxMp = 1000;
            Session.Character.IsSitting = false;
            Buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(Buffs);
        }

        [TestMethod]
        public async Task NoHpConsumptionKeepsPoisonFromTakingHp()
        {
            await new Spec("A poisoned character under a no-HP-consumption card keeps its HP")
                .Given(TheCharacterHas_Hp, 500)
                .AndAsync(TheCharacterHolds_, TimeCircle(BCardEffect.TimeCircleSkillsDisableHpConsumption))
                .AndAsync(TheCharacterIsPoisonedAsync)
                .WhenAsync(FourSecondsPassAsync)
                .Then(TheCharacterShouldHaveLost_Hp, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutTheCardPoisonTakesHp()
        {
            await new Spec("A poisoned character without the card loses HP")
                .Given(TheCharacterHas_Hp, 500)
                .AndAsync(TheCharacterIsPoisonedAsync)
                .WhenAsync(FourSecondsPassAsync)
                .Then(TheCharacterShouldHaveLostSomeHp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoHpConsumptionKeepsACastFromTakingHp()
        {
            await new Spec("Casting under a no-HP-consumption card costs no HP")
                .Given(TheCharacterHas_Hp, 500)
                .AndAsync(TheCharacterHolds_, TimeCircle(BCardEffect.TimeCircleSkillsDisableHpConsumption))
                .AndAsync(TheCharacterHolds_, new BCardDto
                {
                    Type = BCardEffect.HealingBurningAndCastingDecreaseHpWhenCasting.Type(),
                    SubType = BCardEffect.HealingBurningAndCastingDecreaseHpWhenCasting.SubType(),
                    FirstData = 40,
                })
                .WhenAsync(CastingASkillAsync)
                .Then(TheCharacterShouldHaveLost_Hp, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoHpRecoveryKeepsAHealingCardFromHealing()
        {
            await new Spec("A healing card does not heal a character under a no-HP-recovery card")
                .Given(TheCharacterHas_Hp, 500)
                .AndAsync(TheCharacterHolds_, TimeCircle(BCardEffect.TimeCircleSkillsDisableHpRecovery))
                .AndAsync(TheCharacterHolds_, new BCardDto
                {
                    Type = BCardEffect.RecoveryAndDamagePercentHpRecovered.Type(),
                    SubType = BCardEffect.RecoveryAndDamagePercentHpRecovered.SubType(),
                    FirstData = 10, ThirdData = 1,
                })
                .WhenAsync(FourSecondsPassAsync)
                .Then(TheCharacterShouldHaveLost_Hp, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoHpRecoveryStopsRegeneration()
        {
            await new Spec("A character under a no-HP-recovery card does not regenerate HP")
                .Given(TheCharacterHas_Hp, 1)
                .AndAsync(TheCharacterHolds_, TimeCircle(BCardEffect.TimeCircleSkillsDisableHpRecovery))
                .WhenAsync(RegenerationRunsAsync)
                .Then(TheCharacterShouldHaveLost_Hp, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoMpRecoveryStopsRegenerationButNotHp()
        {
            await new Spec("Under a no-MP-recovery card only HP regenerates")
                .Given(TheCharacterHas_Hp, 1)
                .And(TheCharacterHas_Mp, 1)
                .AndAsync(TheCharacterHolds_, TimeCircle(BCardEffect.TimeCircleSkillsDisableMpRecovery))
                .WhenAsync(RegenerationRunsAsync)
                .Then(TheCharacterShouldHaveRegainedHp)
                .And(TheCharacterShouldStillHave_Mp, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoMpConsumptionMakesSkillsFree()
        {
            await new Spec("Under a no-MP-consumption card a skill costs no MP")
                .Given(TheCharacterHas_Mp, 1000)
                .AndAsync(TheCharacterHolds_, TimeCircle(BCardEffect.TimeCircleSkillsDisableMpConsumption))
                .When(CheckingASkillThatCosts_Mp, 50)
                .Then(TheSkillShouldCost_Mp, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutTheCardASkillCostsItsMp()
        {
            await new Spec("Without the card a skill costs its MP")
                .Given(TheCharacterHas_Mp, 1000)
                .When(CheckingASkillThatCosts_Mp, 50)
                .Then(TheSkillShouldCost_Mp, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoHpRecoveryStillLetsPoisonTakeHpBesideAHeal()
        {
            await new Spec("Under a no-HP-recovery card a heal is blocked but poison still takes HP")
                .Given(TheCharacterHas_Hp, 500)
                .AndAsync(TheCharacterHolds_, TimeCircle(BCardEffect.TimeCircleSkillsDisableHpRecovery))
                .AndAsync(TheCharacterHolds_, PercentHp(BCardEffect.RecoveryAndDamagePercentHpRecovered, 20))
                .AndAsync(TheCharacterIsPoisonedAsync)
                .WhenAsync(FourSecondsPassAsync)
                .Then(TheCharacterShouldHaveLostSomeHp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoHpConsumptionStillLetsAHealWorkBesidePoison()
        {
            await new Spec("Under a no-HP-consumption card poison is blocked but a weaker heal still heals")
                .Given(TheCharacterHas_Hp, 500)
                .AndAsync(TheCharacterHolds_, TimeCircle(BCardEffect.TimeCircleSkillsDisableHpConsumption))
                .AndAsync(TheCharacterHolds_, PercentHp(BCardEffect.RecoveryAndDamagePercentHpRecovered, 5))
                .AndAsync(TheCharacterIsPoisonedAsync)
                .WhenAsync(FourSecondsPassAsync)
                .Then(TheCharacterShouldHaveRegainedHp)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoMpConsumptionKeepsAnMpLossFromTakingMp()
        {
            await new Spec("A card that takes a share of MP takes none under a no-MP-consumption card")
                .Given(TheCharacterHas_Mp, 200)
                .AndAsync(TheCharacterHolds_, TimeCircle(BCardEffect.TimeCircleSkillsDisableMpConsumption))
                .AndAsync(TheCharacterHolds_, MpLoss())
                .WhenAsync(FourSecondsPassAsync)
                .Then(TheCharacterShouldStillHave_Mp, 200)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutTheCardAnMpLossTakesMp()
        {
            await new Spec("A card that takes a share of MP takes it without the card")
                .Given(TheCharacterHas_Mp, 200)
                .AndAsync(TheCharacterHolds_, MpLoss())
                .WhenAsync(FourSecondsPassAsync)
                .Then(TheCharacterShouldHaveLostSomeMp)
                .ExecuteAsync();
        }

        private static BCardDto PercentHp(BCardEffect effect, int percent) => new()
        {
            Type = effect.Type(), SubType = effect.SubType(), FirstData = percent, ThirdData = 1,
        };

        private static BCardDto MpLoss() => new()
        {
            Type = BCardEffect.ReflectionRemainingMpLostChance.Type(),
            SubType = BCardEffect.ReflectionRemainingMpLostChance.SubType(),
            FirstData = 100, SecondData = 50, ThirdData = 1,
        };

        private static BCardDto TimeCircle(BCardEffect effect) => new()
        {
            Type = effect.Type(), SubType = effect.SubType(),
        };

        private void TheCharacterHas_Hp(int hp)
        {
            Session.Character.Hp = hp;
            HpBefore = hp;
        }

        private void TheCharacterHas_Mp(int mp)
        {
            Session.Character.Mp = mp;
            MpBefore = mp;
        }

        private Task TheCharacterHolds_(BCardDto bCard) =>
            Buffs.ApplyAsync(Session.Character, new CardDto { CardId = (short)(10 + (int)bCard.Type * 100 + bCard.SubType), Duration = 600 },
                [bCard], null);

        private Task TheCharacterIsPoisonedAsync() =>
            Buffs.ApplyAsync(Session.Character, Poison, [new BCardDto
            {
                Type = BCardEffect.RecoveryAndDamagePercentHpReduced.Type(),
                SubType = BCardEffect.RecoveryAndDamagePercentHpReduced.SubType(),
                FirstData = 10, ThirdData = 1,
            }], null);

        private async Task FourSecondsPassAsync()
        {
            World.Clock.Advance(Duration.FromSeconds(4));
            await Buffs.TickAsync(Session.Character);
        }

        private Task RegenerationRunsAsync()
        {
            var regeneration = World.Create<RegenerationService>();
            var map = Session.Character.MapInstance;
            return map.Executor.RunAsync(async () =>
            {
                for (var i = 0; i < 2; i++)
                {
                    World.Clock.Advance(Duration.FromSeconds(5));
                    var due = new List<Entity>();
                    new RegenerationSystem().FindDue(map.EcsWorld, World.Clock.GetCurrentInstant(), due);
                    foreach (var entity in due)
                    {
                        await regeneration.RegenerateAsync(new PlayerComponentBundle(entity, map.EcsWorld));
                    }
                }
            });
        }

        private Task CastingASkillAsync()
        {
            var skill = new CharacterSkill
            {
                CharacterId = Session.Character.CharacterId,
                SkillVNum = 1,
                Skill = new SkillDto { SkillVNum = 1, CastId = 1, MpCost = 10 },
            };
            return World.Create<SkillCastService>().SpendAsync(Session.Character, skill, 0);
        }

        private void CheckingASkillThatCosts_Mp(int mp)
        {
            var skill = new CharacterSkill
            {
                CharacterId = Session.Character.CharacterId,
                SkillVNum = 1,
                Skill = new SkillDto { SkillVNum = 1, CastId = 1, MpCost = (short)mp },
            };
            World.Create<SkillCastService>().Check(Session.Character, skill, out MpCost);
        }

        private void TheSkillShouldCost_Mp(int mp) => Assert.AreEqual(mp, MpCost);

        private void TheCharacterShouldHaveLost_Hp(int hp) => Assert.AreEqual(HpBefore - hp, Session.Character.Hp);

        private void TheCharacterShouldHaveLostSomeHp() => Assert.IsTrue(Session.Character.Hp < HpBefore);

        private void TheCharacterShouldHaveRegainedHp() => Assert.IsTrue(Session.Character.Hp > HpBefore);

        private void TheCharacterShouldHaveLostSomeMp() => Assert.IsTrue(Session.Character.Mp < MpBefore);

        private void TheCharacterShouldStillHave_Mp(int mp) => Assert.AreEqual(mp, Session.Character.Mp);
    }
}
