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
    public class ChargeTests
    {
        private static Func<BfPacket, bool> Charge(TestClient bearer, int value) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == bearer.CharacterId
                && packet.Buff is { BuffId: 0 } charge && charge.ChargeValue == value && charge.Duration == value;

        [TestMethod]
        public async Task AChargeIsShownUntilTheNextAttackSpendsIt()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 60, 61, c => c.Class = CharacterClassType.Swordsman);
            await Harness.TeachAsync(player, WorldData.ChargeSkill);
            await Harness.TeachAsync(player, WorldData.SwordsmanSwing);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            var charged = alice.Mark();

            await alice.SendAsync(new UseSkillPacket
            {
                CastId = WorldData.ChargeSkillCastId, TargetVisualType = VisualType.Player, TargetId = alice.CharacterId
            });
            await alice.ExpectSinceAsync(charged, Charge(alice, WorldData.ChargeAttack), "the charge on Alice");

            var (_, cast) = await Steps.HitUntilAsync(alice, alice, WorldData.SturdyMonster, _ => true);

            await alice.ExpectSinceAsync(cast, Charge(alice, 0), "the charge spent by the attack");
        }
    }
}
