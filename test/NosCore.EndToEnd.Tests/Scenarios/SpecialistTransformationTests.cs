//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class SpecialistTransformationTests
    {
        private static readonly TimeSpan TransformingTime = TimeSpan.FromSeconds(5.2);

        [TestMethod]
        public async Task OnlookersSeeTheTransformedGearAndEveryoneSeesTheFinish()
        {
            var player = await Harness.AddPlayerAsync("Alice", configure: c => c.SpPoint = 10_000);
            await Harness.GiveAsync(player, new SpecialistInstance { ItemVNum = WorldData.ChefCard, Amount = 1, SpLevel = 1 },
                NoscorePocketType.Wear, (short)EquipmentType.Sp);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            await alice.SendAsync(new SpTransformPacket { Type = SlPacketType.WearSp });
            await alice.ExpectAsync<DelayPacket>();
            await Task.Delay(TransformingTime);
            var seen = bob.Mark();
            var mine = alice.Mark();

            await alice.SendAsync(new SpTransformPacket { Type = SlPacketType.WearSpAndTransform });

            await bob.ExpectSinceAsync<CModePacket>(seen, p => p.VisualId == alice.CharacterId, "Alice transformed");
            await bob.ExpectSinceAsync<EqPacket>(seen, p => p.VisualId == alice.CharacterId, "Alice's gear on her new look");
            var finish = new GuriPacket
            {
                Type = GuriPacketType.CharacterAnimation,
                Argument = (uint)VisualType.Player,
                EntityId = alice.CharacterId,
                Value = 0,
                SecondValue = 0
            };
            // Read back, the optional SecondArgument takes the entity id, on the expected packet as on the received one.
            PacketAssert.AreEqual((GuriPacket)Wire.Deserialize(Wire.Serialize(finish)),
                await bob.ExpectSinceAsync<GuriPacket>(seen, p => p.Type == GuriPacketType.CharacterAnimation, "the end of Alice's transformation"));
            await alice.ExpectSinceAsync<GuriPacket>(mine, p => p.Type == GuriPacketType.CharacterAnimation, "the end of her own transformation");
            Assert.IsTrue(await alice.NoneSinceAsync<EqPacket>(mine, p => p.VisualId == alice.CharacterId, TimeSpan.FromMilliseconds(500)),
                "Alice is not sent her own gear");
        }
    }
}
