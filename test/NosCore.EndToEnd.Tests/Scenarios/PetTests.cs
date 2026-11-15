//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class PetTests
    {
        private const short SkillOfTheOwner = 200;
        private const short ConfirmSendBack = 5;
        private const short SentBack = 6;
        private const short SendBackBar = 3000;
        private const short NoSkill = -1;
        private const short EverySlot = -1;

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

        private static async Task<long> PetOfAsync(TestClient owner) =>
            (await owner.ExpectSinceAsync<ScpPacket>(0, p => p.PetId == 0 && p.NpcMonsterVNum == WorldData.PetVNum, "her pet in the pet list"))
            .TransportId;

        private static Func<SuPacket, bool> HitBy(long pet) =>
            p => p.VisualType == VisualType.Npc && p.VisualId == pet
                && p.TargetVisualType == VisualType.Monster && p.TargetId == WorldData.SturdyMonster;

        private static Func<PetskiPacket, bool> BarOf(short first) =>
            p => p.FirstSkillVNum == first && p.SecondSkillVNum == NoSkill && p.ThirdSkillVNum == NoSkill;

        private static Task OrderAsync(TestClient owner, long pet, short skill) =>
            owner.SendAsync(new UpetPacket { MateTransportId = pet, TargetType = VisualType.Monster, TargetId = WorldData.SturdyMonster, SkillVNum = skill });

        private static Task PetManagementAsync(TestClient owner, short step, long pet) =>
            owner.SendAsync(new NrunPacket { Runner = NrunRunnerType.PetManagement, Type = step, VisualType = VisualType.Monster, VisualId = pet });

        [TestMethod]
        public async Task AnOwnerEnteringTheWorldIsShownTheSkillBarOfHerTeamPet()
        {
            await using var alice = await EnterWithAPetAsync("Alice");

            await alice.ExpectSinceAsync(0, BarOf(WorldData.PetSkill), "the pet's skill bar");
            await alice.ExpectSinceAsync<PetsrPacket>(0, p => p.Slot == EverySlot, "every slot of it ready");
        }

        [TestMethod]
        public async Task SendingThePetHomeTakesItsSkillBarAway()
        {
            await using var alice = await EnterWithAPetAsync("Alice");
            var pet = await PetOfAsync(alice);
            await alice.ExpectSinceAsync(0, BarOf(WorldData.PetSkill), "the pet's skill bar");
            var sent = alice.Mark();

            await PetManagementAsync(alice, ConfirmSendBack, pet);
            await alice.ExpectSinceAsync<DelayPacket>(sent, p => p.Delay == SendBackBar, "the bar before it leaves");
            await Task.Delay(SendBackBar + 200);
            await PetManagementAsync(alice, SentBack, pet);

            await alice.ExpectSinceAsync<OutPacket>(sent, p => p.VisualType == VisualType.Npc && p.VisualId == pet, "the pet leaving");
            await alice.ExpectSinceAsync(sent, BarOf(NoSkill), "the skill bar going with it");
        }

        [TestMethod]
        public async Task APetSkillIsReadyAgainOnceItHasCooledDown()
        {
            await using var alice = await EnterWithAPetAsync("Alice");
            var pet = await PetOfAsync(alice);
            var ordered = alice.Mark();
            await OrderAsync(alice, pet, WorldData.PetSkill);
            await alice.ExpectSinceAsync(ordered, HitBy(pet), "the first hit");

            await alice.ExpectSinceAsync<PetsrPacket>(ordered, p => p.Slot == 0, "the slot ready again");
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

            var cast = await bob.ExpectSinceAsync<CtPacket>(watched,
                p => p.VisualType == VisualType.Npc && p.VisualId == pet && p.TargetVisualType == VisualType.Monster
                    && p.TargetId == WorldData.SturdyMonster, "the pet's cast");
            Assert.AreEqual((long)WorldData.PetSkill, cast.SkillId, "the cast names the skill");
            var hit = await bob.ExpectSinceAsync(watched, HitBy(pet), "the pet's hit");
            Assert.AreEqual((long)WorldData.PetSkill, hit.SkillVnum, "the hit is the skill that was ordered");
            Assert.AreEqual((long)WorldData.PetSkillCooldown, hit.SkillCooldown, "with the skill's own cooldown");

            var bars = await alice.ExpectSinceAsync<PstPacket>(ordered, p => p.Type == VisualType.Npc && p.VisualId == pet, "the pet's bars");
            Assert.IsTrue(bars.MpLeft < 100, "the skill cost the pet mana");
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
