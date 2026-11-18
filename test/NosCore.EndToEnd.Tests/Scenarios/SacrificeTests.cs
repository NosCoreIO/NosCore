//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class SacrificeTests
    {
        private static Func<BfPacket, bool> Buff(TestClient bearer, short card) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == bearer.CharacterId
                && packet.Buff is { ChargeValue: 0 } buff && buff.BuffId == card && buff.Duration == WorldData.SacrificeCardTime;

        [TestMethod]
        public async Task SacrificeGivesItsCasterACardOfItsOwn()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 15, 90, c => c.Class = CharacterClassType.Swordsman);
            await Harness.TeachAsync(player, WorldData.SacrificeSkill);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 17, 90);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            var cast = bob.Mark();

            await alice.SendAsync(new UseSkillPacket
            {
                CastId = WorldData.SacrificeSkillCastId, TargetVisualType = VisualType.Player, TargetId = bob.CharacterId
            });

            await bob.ExpectSinceAsync(cast, Buff(alice, WorldData.SacrificeSpiritCard), "the caster's own card on Alice");
            await bob.ExpectSinceAsync(cast, Buff(bob, WorldData.SacrificeCircleCard), "the circle on Bob");
            Assert.IsTrue(await bob.NoneSinceAsync(cast, Buff(bob, WorldData.SacrificeSpiritCard), TimeSpan.FromSeconds(1)),
                "Bob does not take the caster's card");
        }
    }
}
