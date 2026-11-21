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
    public class BuffEffectTests
    {
        private static Func<BfePacket, bool> WingsOf(TestClient bearer, Func<short, bool> timeLeft) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == bearer.CharacterId
                && packet.CardId == WorldData.WingsCard && timeLeft(packet.RemainingTime);

        [TestMethod]
        public async Task ACardsLastingEffectIsShownToThoseAroundAndToThoseWhoArriveLater()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 60, 61);
            await Harness.TeachAsync(player, WorldData.WingsSkill);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            await using var bob = await Harness.EnterAsync("Bob", WorldData.Meadow, 61, 61);

            var cast = bob.Mark();
            await alice.SendAsync(new UseSkillPacket
            {
                CastId = WorldData.WingsSkillCastId, TargetVisualType = VisualType.Player, TargetId = alice.CharacterId
            });

            await bob.ExpectSinceAsync(cast, WingsOf(alice, left => left == 0), "the effect started over");
            await bob.ExpectSinceAsync(cast, WingsOf(alice, left => left == WorldData.WingsCardTime), "the effect for the whole card");

            await using var carol = await Harness.EnterAsync("Carol", WorldData.Meadow, 62, 61);
            await carol.ExpectSinceAsync(0, WingsOf(alice, left => left is > 0 and <= WorldData.WingsCardTime), "the effect of the card Alice bears");
        }
    }
}
