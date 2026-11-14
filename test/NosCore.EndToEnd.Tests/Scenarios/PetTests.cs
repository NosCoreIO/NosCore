//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class PetTests
    {
        private const int Npc = 2;
        private const int Monster = 3;
        private const short SkillOfTheOwner = 200;
        private const int PetManagement = 4;
        private const int ConfirmSendBack = 5;
        private const int SentBack = 6;
        private const int SendBackBar = 3000;
        private const string NoBar = "petski -1 -1 -1";
        private const string EverySlotReady = "petsr -1";
        private const string FirstSlotReady = "petsr 0";
        private static readonly string BarOfThePet = $"petski {WorldData.PetSkill} -1 -1";

        // The owner stands a cell from the monster, which puts the pet that came in at her side on it.
        private static async Task<TestClient> EnterWithAPetAsync(string name)
        {
            var player = await Harness.AddPlayerAsync(name, WorldData.Meadow, 59, 59);
            await Harness.GivePetAsync(player);
            var client = Harness.Client(player);
            try
            {
                await client.EnterWorldAsync(player.Slot);
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }

            return client;
        }

        // sc_p <slot> <creature> <id> ...: the id every other line names the pet by.
        private static async Task<long> PetOfAsync(TestClient owner)
        {
            var listed = await owner.ExpectSinceAsync(0,
                p => p.StartsWith($"sc_p 0 {WorldData.PetVNum} ", StringComparison.Ordinal), "her pet in the pet list");
            return long.Parse(listed.Split(' ')[3], CultureInfo.InvariantCulture);
        }

        // su <type> <id> <target type> <target id> <skill> <cooldown> ...
        private static Func<string, bool> HitBy(long pet) =>
            packet => packet.StartsWith($"su {Npc} {pet} {Monster} {WorldData.SturdyMonster} ", StringComparison.Ordinal);

        private static Task OrderAsync(TestClient owner, long pet, short skill) =>
            owner.SendAsync($"u_pet {pet} {Monster} {WorldData.SturdyMonster} {skill}");

        [TestMethod]
        public async Task AnOwnerEnteringTheWorldIsShownTheSkillBarOfHerTeamPet()
        {
            await using var alice = await EnterWithAPetAsync("Alice");

            await alice.ExpectSinceAsync(0, p => p == BarOfThePet, "the pet's skill bar");
            await alice.ExpectSinceAsync(0, p => p == EverySlotReady, "every slot of it ready");
        }

        [TestMethod]
        public async Task SendingThePetHomeTakesItsSkillBarAway()
        {
            await using var alice = await EnterWithAPetAsync("Alice");
            var pet = await PetOfAsync(alice);
            await alice.ExpectSinceAsync(0, p => p == BarOfThePet, "the pet's skill bar");
            var sent = alice.Mark();

            await alice.SendAsync($"n_run {PetManagement} {ConfirmSendBack} {Monster} {pet}");
            await alice.ExpectSinceAsync(sent, p => p.StartsWith($"delay {SendBackBar} ", StringComparison.Ordinal), "the bar before it leaves");
            await Task.Delay(SendBackBar + 200);
            await alice.SendAsync($"n_run {PetManagement} {SentBack} {Monster} {pet}");

            await alice.ExpectSinceAsync(sent, p => p == $"out {Npc} {pet}", "the pet leaving");
            await alice.ExpectSinceAsync(sent, p => p == NoBar, "the skill bar going with it");
        }

        [TestMethod]
        public async Task APetSkillIsReadyAgainOnceItHasCooledDown()
        {
            await using var alice = await EnterWithAPetAsync("Alice");
            var pet = await PetOfAsync(alice);
            var ordered = alice.Mark();
            await OrderAsync(alice, pet, WorldData.PetSkill);
            await alice.ExpectSinceAsync(ordered, HitBy(pet), "the first hit");

            await alice.ExpectSinceAsync(ordered, p => p == FirstSlotReady, "the slot ready again");
            var again = alice.Mark();
            await OrderAsync(alice, pet, WorldData.PetSkill);

            await alice.ExpectSinceAsync(again, HitBy(pet), "a second hit");
        }

        [TestMethod]
        public async Task APetUsesASkillOfItsOwnOnAMonsterAndEveryoneSeesIt()
        {
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 62, 62);
            await using var alice = await EnterWithAPetAsync("Alice");
            var pet = await PetOfAsync(alice);
            var ordered = alice.Mark();
            var watched = bob.Mark();

            await OrderAsync(alice, pet, WorldData.PetSkill);

            var cast = (await bob.ExpectSinceAsync(watched,
                p => p.StartsWith($"ct {Npc} {pet} {Monster} {WorldData.SturdyMonster} ", StringComparison.Ordinal), "the pet's cast")).Split(' ');
            Assert.AreEqual(WorldData.PetSkill.ToString(CultureInfo.InvariantCulture), cast[^1], "the cast names the skill");
            var hit = (await bob.ExpectSinceAsync(watched, HitBy(pet), "the pet's hit")).Split(' ');
            Assert.AreEqual(WorldData.PetSkill.ToString(CultureInfo.InvariantCulture), hit[5], "the hit is the skill that was ordered");
            Assert.AreEqual(WorldData.PetSkillCooldown.ToString(CultureInfo.InvariantCulture), hit[6], "with the skill's own cooldown");

            // pst <type> <id> <order> <hp %> <mp %> ...
            var bars = (await alice.ExpectSinceAsync(ordered,
                p => p.StartsWith($"pst {Npc} {pet} ", StringComparison.Ordinal), "the pet's bars")).Split(' ');
            Assert.IsTrue(int.Parse(bars[5], CultureInfo.InvariantCulture) < 100, "the skill cost the pet mana");
        }

        [TestMethod]
        public async Task APetSkillIsRefusedUntilItHasCooledDown()
        {
            await using var alice = await EnterWithAPetAsync("Alice");
            var pet = await PetOfAsync(alice);
            await OrderAsync(alice, pet, WorldData.PetSkill);
            await alice.ExpectAsync(HitBy(pet), "the first hit");
            var again = alice.Mark();

            await OrderAsync(alice, pet, WorldData.PetSkill);

            Assert.IsTrue(await alice.NoneSinceAsync(again, HitBy(pet), TimeSpan.FromSeconds(1)),
                "the pet does not use the skill a second time while it cools down");
        }

        [TestMethod]
        public async Task APetOnlyUsesTheSkillsOfItsOwnBar()
        {
            await using var alice = await EnterWithAPetAsync("Alice");
            var pet = await PetOfAsync(alice);
            var ordered = alice.Mark();

            await OrderAsync(alice, pet, SkillOfTheOwner);

            Assert.IsTrue(await alice.NoneSinceAsync(ordered, HitBy(pet), TimeSpan.FromSeconds(1)),
                "a skill that is not the pet's own is not cast");
        }
    }
}
