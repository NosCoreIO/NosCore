//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class FightTests
    {
        private const int MostHp = 221;
        private const int MostMp = 60;
        private const int RangedSkill = 1;
        private const int RangedSkillRange = 7;

        private static Func<SuPacket, bool> HitBy(TestClient attacker, int monster) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == attacker.CharacterId
                && packet.TargetVisualType == VisualType.Monster && packet.TargetId == monster;

        private static Func<SuPacket, bool> HitOn(TestClient victim, int monster) =>
            packet => packet.VisualType == VisualType.Monster && packet.VisualId == monster
                && packet.TargetVisualType == VisualType.Player && packet.TargetId == victim.CharacterId;

        private static UseSkillPacket Cast(long castId, int monster) =>
            new() { CastId = castId, TargetVisualType = VisualType.Monster, TargetId = monster };

        private static Func<DlgiPacket, bool> AskedHowToRevive =>
            packet => packet.YesPacket is RevivalPacket { Type: RevivalType.InPlace } && packet.NoPacket is RevivalPacket { Type: RevivalType.AtSavePoint };

        private static Func<CancelPacket, bool> Refused => packet => packet.Type == CancelPacketType.CancelAutoAttack;

        private static Func<CMapPacket, bool> InTown => packet => packet is { Type: 0, Id: WorldData.Town, IsEntering: true };

        // A hit may miss, and a monster that was missed does not mind: she hits until one lands.
        private static async Task<int> ProvokeAsync(TestClient attacker)
        {
            var provoked = attacker.Mark();
            await Steps.HitUntilAsync(attacker, attacker, WorldData.Biter, hit => hit.Damage != 0);
            return provoked;
        }

        // With little HP left, hitting the biter is a sure way to die.
        private static async Task<int> DieAsync(TestClient victim)
        {
            var provoked = await ProvokeAsync(victim);
            await victim.ExpectSinceAsync<SuPacket>(provoked, p => HitOn(victim, WorldData.Biter)(p) && !p.TargetIsAlive, "the blow that kills",
                TimeSpan.FromSeconds(20));
            await victim.ExpectSinceAsync(provoked, AskedHowToRevive, "the question how to revive");
            return provoked;
        }

        [TestMethod]
        public async Task AMonsterThatIsHitHitsBackAndTakesHp()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 80, 21);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 82, 22);
            Assert.IsTrue(await alice.NoneSinceAsync(0, HitOn(alice, WorldData.Biter), TimeSpan.FromSeconds(1)), "left alone, it leaves her alone");

            var hit = await ProvokeAsync(alice);

            var blow = await bob.ExpectAsync<SuPacket>(p => HitOn(alice, WorldData.Biter)(p) && p.Damage != 0, "the biter hitting back",
                TimeSpan.FromSeconds(20));
            var damage = (int)blow.Damage;
            Assert.IsTrue(blow.TargetIsAlive, "she is still alive");
            Assert.AreEqual((MostHp - damage) * 100 / MostHp, blow.HpPercentage, "the part of her HP she has left");
            var stat = await alice.ExpectSinceAsync<StatPacket>(hit, p => p.Hp < MostHp, "her HP after the blow");
            Assert.AreEqual(MostHp - damage, stat.Hp);
        }

        [TestMethod]
        public async Task ACharacterWhoseHpRunsOutDiesAndIsAskedHowToRevive()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 80, 21, c => c.Hp = 1);

            var died = await DieAsync(alice);

            await alice.ExpectSinceAsync<StatPacket>(died, p => p.Hp == 0 && p.Mp == 0, "neither HP nor MP left");
            var question = alice.Since<DlgiPacket>(died).First();
            Assert.IsTrue(AskedHowToRevive(question), "revive here or at the save point");
            Assert.AreEqual(Game18NConstString.ContinueHereFree, question.Question);
        }

        [TestMethod]
        public async Task TheDeadNeitherWalkNorFightNorDrinkPotions()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 80, 21, c => c.Hp = 1);
            await Harness.GiveAsync(player, WorldData.Potion, 2, pocket: NoscorePocketType.Main);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 82, 22);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            await DieAsync(alice);
            var dead = alice.Mark();
            var watched = bob.Mark();

            await alice.WalkAsync(80, 22);
            await alice.SendAsync(Cast(0, WorldData.Biter));
            await alice.SendAsync(new UseItemPacket
            {
                VisualType = VisualType.Player, VisualId = alice.CharacterId, Type = PocketType.Main, Slot = 0, Mode = 0, Parameter = 0
            });

            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(watched, p => p is MovePacket { VisualType: VisualType.Player } step
                && step.VisualEntityId == alice.CharacterId || p is SuPacket hit && HitBy(alice, WorldData.Biter)(hit), TimeSpan.FromSeconds(2)),
                "she neither walks nor hits");
            Assert.IsFalse(alice.Since(dead).Any(p => p is IvnPacket or StatPacket { Hp: > 0 }), "no potion is drunk");
        }

        [TestMethod]
        public async Task RevivingOnTheSpotGivesBackAllHpAndIsSeenByBystanders()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 80, 21, c => c.Hp = 1);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 82, 22);
            await DieAsync(alice);
            var revived = alice.Mark();
            var watched = bob.Mark();

            await alice.SendAsync(new RevivalPacket { Type = RevivalType.InPlace });

            await bob.ExpectSinceAsync<RevivePacket>(watched, p => p is { VisualType: VisualType.Player, Data: 0 } && p.VisualId == alice.CharacterId,
                "her revival");
            await bob.ExpectSinceAsync<TpPacket>(watched, p => p is { VisualType: VisualType.Player, X: 80, Y: 21, Unknown: 0 }
                && p.VisualId == alice.CharacterId, "her standing where she fell");
            var stat = await alice.ExpectSinceAsync<StatPacket>(revived, p => p.Hp > 0, "her HP after reviving");
            Assert.AreEqual((MostHp, MostMp), (stat.Hp, stat.Mp));

            await alice.WalkAsync(80, 22);
            await bob.ExpectSinceAsync<MovePacket>(watched, p => p is { VisualType: VisualType.Player, MapX: 80, MapY: 22 }
                && p.VisualEntityId == alice.CharacterId, "her walking again");
        }

        [TestMethod]
        public async Task RevivingAtTheSavePointBringsTheCharacterBackToTown()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 80, 21, c => c.Hp = 1);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 82, 22);
            await using (var alice = Harness.Client(player))
            {
                await alice.EnterWorldAsync(player.Slot);
                await DieAsync(alice);
                var revived = alice.Mark();
                var watched = bob.Mark();

                await alice.SendAsync(new RevivalPacket { Type = RevivalType.AtSavePoint });

                await alice.ExpectSinceAsync(revived, InTown, "the town she revives in");
                var stat = await alice.ExpectSinceAsync<StatPacket>(revived, p => p.Hp > 0, "her HP after reviving");
                Assert.AreEqual((MostHp, MostMp), (stat.Hp, stat.Mp));
                await bob.ExpectSinceAsync<OutPacket>(watched, p => p.VisualType == VisualType.Player && p.VisualId == alice.CharacterId,
                    "her leaving the meadow");
            }

            await using var back = await Harness.ComeBackAsync(player);
            var stored = await Harness.StoredAsync(player.CharacterId);
            Assert.AreEqual(WorldData.Town, stored.MapId, "she is stored in town");
            Assert.AreEqual(MostHp, stored.Hp, "and alive");
        }

        [TestMethod]
        public async Task ACharacterWhoLeavesTheWorldDeadIsStoredDead()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 80, 21, c => c.Hp = 1);
            await using (var alice = Harness.Client(player))
            {
                await alice.EnterWorldAsync(player.Slot);
                await DieAsync(alice);
            }

            var waited = Stopwatch.StartNew();
            while ((await Harness.StoredAsync(player.CharacterId)).Hp != 0 && waited.Elapsed < TimeSpan.FromSeconds(15))
            {
                await Task.Delay(250);
            }

            Assert.AreEqual(0, (await Harness.StoredAsync(player.CharacterId)).Hp, "leaving does not bring her back to life");
        }

        // The harness cannot bring such a character in the usual way: that waits for the map it was stored on.
        [TestMethod]
        public async Task ACharacterStoredDeadComesBackAtItsSavePointBarelyAlive()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 82, 30, c => c.Hp = 0);
            await using var alice = Harness.Client(player);
            await alice.ConnectAsync();
            var entered = alice.Mark();
            await alice.SendAsync(new SelectPacket { Slot = player.Slot });
            await alice.ExpectAsync<OkPacket>();

            await alice.SendAsync(new GameStartPacket());
            await alice.SendAsync(new LbsPacket { Type = 0 });

            await alice.ExpectSinceAsync(entered, InTown, "the town she comes back in");
            var bars = await alice.ExpectSinceAsync<StatPacket>(entered, null, "her bars on entering");
            Assert.AreEqual((1, 1), (bars.Hp, bars.Mp), "one point of each");
            Assert.IsFalse(alice.Since<DlgiPacket>(entered).Any(AskedHowToRevive), "nobody asks her how to revive");
        }

        [TestMethod]
        public async Task TheLivingCannotReviveThemselvesBackToFullHp()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 82, 24, c => c.Hp = 100);
            var asked = alice.Mark();

            await alice.SendAsync(new RevivalPacket { Type = RevivalType.InPlace });
            await alice.SendAsync(new RevivalPacket { Type = RevivalType.AtSavePoint });

            Assert.IsTrue(await alice.NoneSinceAsync<IPacket>(asked, p => p is StatPacket { Hp: > 100 } or RevivePacket or CMapPacket,
                TimeSpan.FromSeconds(1)), "nothing is restored and she stays where she is");
        }

        [TestMethod]
        public async Task ARangedSkillReachesAMonsterWithinItsRangeOnly()
        {
            await using var near = await Harness.EnterAsync("Alice", WorldData.Meadow, 60, (short)(60 + RangedSkillRange));
            await using var far = await Harness.EnterAsync("Bob", WorldData.Meadow, 60, (short)(60 + RangedSkillRange + 5));
            var refused = far.Mark();
            var watched = near.Mark();

            await far.SendAsync(Cast(RangedSkill, WorldData.SturdyMonster));

            await far.ExpectSinceAsync(refused, Refused, "the refusal");
            Assert.IsTrue(await near.NoneSinceAsync(watched, HitBy(far, WorldData.SturdyMonster), TimeSpan.FromSeconds(1)), "no hit from out of range");

            await near.SendAsync(Cast(RangedSkill, WorldData.SturdyMonster));

            var hit = await far.ExpectAsync(HitBy(near, WorldData.SturdyMonster), "the hit from within range");
            Assert.AreEqual(201, hit.SkillVnum, "the ranged skill");
        }

        [TestMethod]
        public async Task AMeleeSkillDoesNotReachAMonsterStandingApart()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 60, 64);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 62, 62);
            var refused = alice.Mark();
            var watched = bob.Mark();

            await alice.SendAsync(Cast(0, WorldData.SturdyMonster));

            await alice.ExpectSinceAsync(refused, Refused, "the refusal");
            Assert.IsTrue(await bob.NoneSinceAsync(watched, HitBy(alice, WorldData.SturdyMonster), TimeSpan.FromSeconds(1)), "no hit from four cells away");
        }

        [TestMethod]
        public async Task ASkillCostsItsMpAndIsRefusedUntilItHasCooledDown()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 60, 61);
            await Harness.TeachAsync(player, WorldData.SlowSkill);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 62, 62);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            var cast = alice.Mark();

            await alice.SendAsync(Cast(WorldData.SlowSkillCastId, WorldData.SturdyMonster));

            var hit = await bob.ExpectAsync(HitBy(alice, WorldData.SturdyMonster), "the hit");
            Assert.AreEqual(WorldData.SlowSkill, hit.SkillVnum);
            Assert.AreEqual(MostMp - WorldData.SlowSkillMp, (await alice.ExpectSinceAsync<StatPacket>(cast, p => p.Mp < MostMp, "the MP it cost")).Mp);
            var again = alice.Mark();
            var watched = bob.Mark();

            await alice.SendAsync(Cast(WorldData.SlowSkillCastId, WorldData.SturdyMonster));

            await alice.ExpectSinceAsync(again, Refused, "the refusal");
            Assert.IsTrue(await bob.NoneSinceAsync(watched, HitBy(alice, WorldData.SturdyMonster), TimeSpan.FromSeconds(1)), "no second hit");
            Assert.IsFalse(alice.Since<StatPacket>(again).Any(p => p.Mp < MostMp - WorldData.SlowSkillMp), "and no MP spent on it");
        }

        [TestMethod]
        public async Task ASkillIsRefusedToSomeoneWithoutTheMpForIt()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 60, 61, c => c.Mp = WorldData.SlowSkillMp - 1);
            await Harness.TeachAsync(player, WorldData.SlowSkill);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 62, 62);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            var refused = alice.Mark();
            var watched = bob.Mark();

            await alice.SendAsync(Cast(WorldData.SlowSkillCastId, WorldData.SturdyMonster));

            await alice.ExpectSinceAsync(refused, Refused, "the refusal");
            Assert.IsTrue(await bob.NoneSinceAsync(watched, HitBy(alice, WorldData.SturdyMonster), TimeSpan.FromSeconds(1)), "no hit");
        }

        [TestMethod]
        public async Task AnotherPlayerCannotBeHitOnAnOrdinaryMap()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, 84, 40);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 84, 41);
            var struck = bob.Mark();

            await alice.SendAsync(new UseSkillPacket { CastId = 0, TargetVisualType = VisualType.Player, TargetId = bob.CharacterId });

            Assert.IsTrue(await bob.NoneSinceAsync<IPacket>(struck, p => p is SuPacket { VisualType: VisualType.Player, TargetVisualType: VisualType.Player } hit
                && hit.VisualId == alice.CharacterId && hit.TargetId == bob.CharacterId || p is StatPacket { Hp: < MostHp }, TimeSpan.FromSeconds(2)),
                "Bob is not hit");
        }
    }
}
