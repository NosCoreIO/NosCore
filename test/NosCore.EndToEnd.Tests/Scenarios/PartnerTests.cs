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
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class PartnerTests
    {
        private static readonly TimeSpan TransformBar = TimeSpan.FromSeconds(5.2);

        // The owner comes in with her partner at her side, wearing its card, and has it take the card's look.
        private static async Task<(TestClient Owner, long Partner)> EnterWithATransformedPartnerAsync(string name)
        {
            var player = await Harness.AddPlayerAsync(name, WorldData.Meadow, 59, 59);
            await Harness.GivePartnerAsync(player);
            var owner = Harness.Client(player);
            try
            {
                await owner.EnterWorldAsync(player.Slot);
                var partner = (await owner.ExpectSinceAsync<ScnPacket>(0, p => p.NpcMonsterVNum == WorldData.PartnerVNum, "her partner"))
                    .TransportId;
                var toggled = owner.Mark();
                await owner.SendAsync(new PslPacket { Type = PslPacketType.Unwear });
                await owner.ExpectSinceAsync<PdelayPacket>(toggled, null, "the transformation bar");
                await Task.Delay(TransformBar);
                await owner.SendAsync(new PslPacket { Type = PslPacketType.Wear });
                await owner.ExpectSinceAsync<PSkiPacket>(toggled, null, "the partner skill bar");
                return (owner, partner);
            }
            catch
            {
                await owner.DisposeAsync();
                throw;
            }
        }

        private static Task CastOnItselfAsync(TestClient owner, long partner) =>
            owner.SendAsync(new UpsPacket { MateTransportId = partner, TargetType = VisualType.Npc, TargetId = partner, SkillSlot = 0 });

        private static Func<SuPacket, bool> CastOnItself(long partner) =>
            p => p.VisualType == VisualType.Npc && p.VisualId == partner && p.TargetVisualType == VisualType.Npc && p.TargetId == partner
                && p.SkillVnum == WorldData.PartnerSkill;

        [TestMethod]
        public async Task APartnerSkillCentredOnThePartnerBuffsItAndCostsItsMana()
        {
            var (alice, partner) = await EnterWithATransformedPartnerAsync("Alice");
            await using var owner = alice;
            var cast = owner.Mark();

            await CastOnItselfAsync(owner, partner);

            await owner.ExpectSinceAsync(cast, CastOnItself(partner), "the skill landing on the partner");
            await owner.ExpectSinceAsync<BfPacket>(cast,
                p => p.VisualType == VisualType.Npc && p.VisualId == partner && p.Buff is { } buff && buff.BuffId == WorldData.PartnerBuff
                    && buff.Duration > 0, "the partner's buff");
            var bars = await owner.ExpectSinceAsync<PstPacket>(cast, p => p.Type == VisualType.Npc && p.VisualId == partner && p.MpLeft < 100,
                "the partner's bars after the mana it spent");
            Assert.IsTrue(bars.Buffs?.Any(b => b?.CardId == WorldData.PartnerBuff) == true, "the buff shows under the partner");
        }

        [TestMethod]
        public async Task APartnerSkillCentredOnThePartnerWaitsForItsCooldown()
        {
            var (alice, partner) = await EnterWithATransformedPartnerAsync("Alice");
            await using var owner = alice;
            await CastOnItselfAsync(owner, partner);
            await owner.ExpectAsync(CastOnItself(partner), "the first cast");
            var again = owner.Mark();

            await CastOnItselfAsync(owner, partner);

            Assert.IsTrue(await owner.NoneSinceAsync(again, CastOnItself(partner), TimeSpan.FromSeconds(1)), "no second cast");
        }
    }
}
