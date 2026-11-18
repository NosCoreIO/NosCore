//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // A debuff whose drain carries an interval moves HP or MP from its bearer to whoever
    // inflicted it at every step: Evil Potion (card 159, BCard 51/21), Mana Drain (card 228,
    // 21/42), and Lich Magic (cards 653 and 654, 20/52) burns a share of the remaining MP.
    [TestClass]
    public class PeriodicDrainTests
    {
        private static readonly CardDto Debuff = new() { CardId = 159, Duration = 200 };

        private TestWorld World;
        private ClientSession Session;
        private BuffService Buffs;
        private MonsterComponentBundle Bearer;
        private int CasterHpBefore;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Buffs = World.Create<BuffService>();
        }

        [TestMethod]
        public async Task EvilPotionMovesHpFromTheBearerToTheCaster()
        {
            await new Spec("Every step the bearer of Evil Potion loses 7 HP per level of its caster and the caster gains them")
                .Given(AMonsterWith_Hp, 5000)
                .And(ACasterOfLevel_AtOneHp, (byte)10)
                .AndAsync(TheCasterInflicts_, Card(BCardEffect.DrainTransferEnemyHp, 7, levelScaled: true))
                .WhenAsync(TwoSecondsPassAsync)
                .Then(TheBearerShouldHave_Hp, 5000 - 70)
                .And(TheCasterShouldHaveGained_Hp, 70)
                .And(TheMapShouldSeeTheBearerLose_, 70)
                .And(TheMapShouldSeeTheCasterHealed_, 70)
                .And(TheCasterShouldHaveBeenSentItsStat)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheDrainLeavesTheBearerOneHpAndGivesOnlyWhatItTook()
        {
            await new Spec("A bearer with less HP than the drain stays at 1 HP and the caster gets what was taken")
                .Given(AMonsterWith_Hp, 5000)
                .And(TheBearerIsDownTo_Hp, 50)
                .And(ACasterOfLevel_AtOneHp, (byte)10)
                .AndAsync(TheCasterInflicts_, Card(BCardEffect.DrainTransferEnemyHp, 7, levelScaled: true))
                .WhenAsync(TwoSecondsPassAsync)
                .Then(TheBearerShouldHave_Hp, 1)
                .And(TheCasterShouldHaveGained_Hp, 49)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeadCasterGainsNothingButTheBearerStillLoses()
        {
            await new Spec("When the caster has died the bearer still loses the HP and nobody gains it")
                .Given(AMonsterWith_Hp, 5000)
                .And(ACasterOfLevel_AtOneHp, (byte)10)
                .AndAsync(TheCasterInflicts_, Card(BCardEffect.DrainTransferEnemyHp, 7, levelScaled: true))
                .And(TheCasterDies)
                .WhenAsync(TwoSecondsPassAsync)
                .Then(TheBearerShouldHave_Hp, 5000 - 70)
                .And(TheCasterShouldHaveGained_Hp, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ManaDrainHandsTheBearersMpToTheCaster()
        {
            await new Spec("Every step Mana Drain takes its MP from the bearer and gives it to the caster")
                .Given(AMonsterWith_Hp, 5000)
                .And(TheBearerHas_Mp, 10)
                .And(TheCasterHasNoMp)
                .AndAsync(TheCasterInflicts_, Card(BCardEffect.DrainAndStealLeechEnemyMp, 100, second: 3))
                .WhenAsync(TwoSecondsPassAsync)
                .Then(TheBearerShouldHave_Mp, 7)
                .And(TheCasterShouldHave_Mp, 3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LichMagicBurnsAShareOfTheRemainingMpEveryStep()
        {
            await new Spec("Every step Lich Magic takes 12% of the MP the bearer has left: 1000, then 880, then 775")
                .Given(AMonsterWith_Hp, 5000)
                .And(TheBearerHas_Mp, 1000)
                .AndAsync(TheCasterInflicts_, Card(BCardEffect.ReflectionRemainingMpLostChance, 100, second: 12))
                .WhenAsync(TwoSecondsPassAsync)
                .AndAsync(TwoSecondsPassAsync)
                .Then(TheBearerShouldHave_Mp, 775)
                .ExecuteAsync();
        }

        private static BCardDto Card(BCardEffect effect, int first, int second = 0, bool levelScaled = false) => new()
        {
            Type = effect.Type(), SubType = effect.SubType(), FirstData = first, SecondData = second,
            ThirdData = 1, IsLevelScaled = levelScaled,
        };

        private void AMonsterWith_Hp(int maxHp)
        {
            var map = Session.Character.MapInstance;
            var creature = new NpcMonsterDto { NpcMonsterVNum = 333, Level = 1, MaxHp = maxHp, MaxMp = 1000 };
            Bearer = new MonsterComponentBundle(
                map.EcsWorld.CreateMonster(9001, creature, map, 12, 14, 2, 12, 14, false, true, false), map.EcsWorld);
        }

        private void TheBearerIsDownTo_Hp(int hp) => Bearer.Hp = hp;

        private void TheBearerHas_Mp(int mp) => Bearer.Mp = mp;

        private void ACasterOfLevel_AtOneHp(byte level)
        {
            Session.Character.Level = level;
            Session.Character.Hp = 1;
            CasterHpBefore = Session.Character.Hp;
        }

        private void TheCasterHasNoMp() => Session.Character.Mp = 0;

        private void TheCasterDies() => Session.Character.IsAlive = false;

        private Task TheCasterInflicts_(BCardDto bCard) => Buffs.ApplyAsync(Bearer, Debuff, [bCard], Session.Character);

        private async Task TwoSecondsPassAsync()
        {
            Session.Character.MapInstance.LastPackets.Clear();
            Session.LastPackets.Clear();
            World.Clock.Advance(Duration.FromSeconds(2));
            await Buffs.TickAsync(Bearer);
        }

        private void TheBearerShouldHave_Hp(int hp) => Assert.AreEqual(hp, Bearer.Hp);

        private void TheBearerShouldHave_Mp(int mp) => Assert.AreEqual(mp, Bearer.Mp);

        private void TheCasterShouldHaveGained_Hp(int hp) => Assert.AreEqual(CasterHpBefore + hp, Session.Character.Hp);

        private void TheCasterShouldHave_Mp(int mp) => Assert.AreEqual(mp, Session.Character.Mp);

        private void TheMapShouldSeeTheBearerLose_(int hp) =>
            PacketAssert.AreEqual(new DmPacket
            {
                VisualType = VisualType.Monster, VisualId = Bearer.VisualId, Damages = hp, DamageType = DamagePacketType.Damage,
            }, Session.Character.MapInstance.LastPackets.OfType<DmPacket>().Single());

        private void TheMapShouldSeeTheCasterHealed_(int hp) =>
            PacketAssert.AreEqual(new RcPacket
            {
                VisualType = VisualType.Player, VisualId = Session.Character.VisualId, HealAmount = hp, Unknown = 0,
            }, Session.Character.MapInstance.LastPackets.OfType<RcPacket>().Single());

        private void TheCasterShouldHaveBeenSentItsStat() =>
            Assert.AreEqual(Session.Character.Hp, Session.LastPackets.OfType<StatPacket>().Last().Hp);
    }
}
