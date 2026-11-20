//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class ExplosionTests
    {
        private const int MostHp = 221;

        [TestMethod]
        public async Task AMonsterThatExplodesHurtsEveryoneAroundItAndIsGone()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Meadow, WorldData.WigglerX, WorldData.WigglerY + 1);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, WorldData.WigglerX + 1, WorldData.WigglerY + 2);
            var before = bob.Mark();

            await Steps.HitUntilAsync(alice, alice, WorldData.Wiggler, hit => hit.Damage != 0);

            var blast = await bob.ExpectSinceAsync<SuPacket>(before,
                p => p.VisualType == VisualType.Monster && p.VisualId == WorldData.Wiggler
                    && p.TargetVisualType == VisualType.Player && p.TargetId == bob.CharacterId,
                "the blast reaching Bob", TimeSpan.FromSeconds(20));
            Assert.AreEqual((uint)WorldData.WigglerBlast, blast.Damage);
            Assert.AreEqual(SuPacketHitMode.AttackedInAoe, blast.HitMode);
            var stat = await bob.ExpectSinceAsync<StatPacket>(before, p => p.Hp < MostHp, "Bob's HP after the blast");
            Assert.AreEqual(MostHp - WorldData.WigglerBlast, stat.Hp);
            PacketAssert.AreEqual(new DiePacket
            {
                VisualType = VisualType.Monster,
                VisualId = WorldData.Wiggler,
                TargetVisualType = VisualType.Monster,
                TargetId = WorldData.Wiggler,
            }, await bob.ExpectSinceAsync<DiePacket>(before, p => p.VisualId == WorldData.Wiggler, "the wiggler gone"));
        }
    }
}
