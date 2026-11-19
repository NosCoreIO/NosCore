//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class CleansingTests
    {
        private static UseSkillPacket OnHerself(TestClient caster, long castId) =>
            new() { CastId = castId, TargetVisualType = VisualType.Player, TargetId = caster.CharacterId };

        private static Func<BfPacket, bool> Bleeding(TestClient bearer, short card, bool lasting) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == bearer.CharacterId
                && packet.Buff is { } buff && buff.BuffId == card && buff.Duration > 0 == lasting;

        [TestMethod]
        public async Task ABandageStopsTheBleedingsUpToItsLevelAndLeavesTheHeavierOnes()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 60, 61);
            await Harness.TeachAsync(player, WorldData.WoundSkill);
            await Harness.TeachAsync(player, WorldData.StopBleedingSkill);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);

            var wounded = alice.Mark();
            await alice.SendAsync(OnHerself(alice, WorldData.WoundSkillCastId));
            await alice.ExpectSinceAsync(wounded, Bleeding(alice, WorldData.LightBleeding, true), "the light bleeding");
            await alice.ExpectSinceAsync(wounded, Bleeding(alice, WorldData.HeavyBleeding, true), "the heavy bleeding");

            var bandaged = alice.Mark();
            await alice.SendAsync(OnHerself(alice, WorldData.StopBleedingSkillCastId));

            await alice.ExpectSinceAsync(bandaged, Bleeding(alice, WorldData.LightBleeding, false), "the light bleeding stopped");
            Assert.IsTrue(await alice.NoneSinceAsync(bandaged, Bleeding(alice, WorldData.HeavyBleeding, false), TimeSpan.FromSeconds(1)),
                "the heavy bleeding goes on");
        }

        [TestMethod]
        public async Task ABandageUsedFromTheBagStopsTheLighterBleedingAndIsUsedUp()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 60, 61);
            await Harness.GiveAsync(player, WorldData.Bandage, 2);
            await Harness.TeachAsync(player, WorldData.WoundSkill);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);

            var wounded = alice.Mark();
            await alice.SendAsync(OnHerself(alice, WorldData.WoundSkillCastId));
            await alice.ExpectSinceAsync(wounded, Bleeding(alice, WorldData.LightBleeding, true), "the light bleeding");
            await alice.ExpectSinceAsync(wounded, Bleeding(alice, WorldData.HeavyBleeding, true), "the heavy bleeding");

            var bandaged = alice.Mark();
            await alice.SendAsync(new UseItemPacket
            {
                VisualType = VisualType.Player, VisualId = alice.CharacterId, Type = PocketType.Etc, Slot = 0, Mode = 0, Parameter = 0
            });

            await alice.ExpectSinceAsync(bandaged, Bleeding(alice, WorldData.LightBleeding, false), "the light bleeding stopped");
            await alice.ExpectSinceAsync<IvnPacket>(bandaged, p => p.Type == PocketType.Etc
                && p.IvnSubPackets?.Any(item => item.VNum == WorldData.Bandage && item.RareAmount == 1) == true, "one bandage left in the bag");
            Assert.IsTrue(await alice.NoneSinceAsync(bandaged, Bleeding(alice, WorldData.HeavyBleeding, false), TimeSpan.FromSeconds(1)),
                "the heavy bleeding goes on");
        }
    }
}
