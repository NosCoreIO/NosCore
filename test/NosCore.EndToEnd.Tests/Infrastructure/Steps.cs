//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Infrastructure
{
    public static class Steps
    {
        // A hit may miss or leave the monster standing: the attacker hits until one does what is wanted, and that one
        // is returned as the watcher saw it, with the attacker's mark from just before it.
        public static async Task<(SuPacket Hit, int Cast)> HitUntilAsync(TestClient attacker, TestClient watcher, long monster,
            Func<SuPacket, bool> wanted)
        {
            var tried = Stopwatch.StartNew();
            while (true)
            {
                var seen = watcher.Mark();
                var cast = attacker.Mark();
                await attacker.SendAsync(new UseSkillPacket { CastId = 0, TargetVisualType = VisualType.Monster, TargetId = monster });
                var hit = await watcher.ExpectSinceAsync<SuPacket>(seen,
                    p => p.VisualType == VisualType.Player && p.VisualId == attacker.CharacterId
                        && p.TargetVisualType == VisualType.Monster && p.TargetId == monster, "the hit");
                if (wanted(hit))
                {
                    return (hit, cast);
                }

                Assert.IsTrue(tried.Elapsed < TimeSpan.FromSeconds(30), "no hit did it in thirty seconds");
                await attacker.ExpectSinceAsync<SkillResetPacket>(cast, p => p.CastId == 0, "the skill ready again");
            }
        }

        public static async Task<(SuPacket Hit, int Cast)> KillAsync(TestClient attacker, long monster) =>
            await HitUntilAsync(attacker, attacker, monster, hit => !hit.TargetIsAlive);
    }
}
