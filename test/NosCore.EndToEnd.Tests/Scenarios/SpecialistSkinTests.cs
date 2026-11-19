//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class SpecialistSkinTests
    {
        // The bar the client waits out before it asks to transform.
        private static readonly TimeSpan TransformingTime = TimeSpan.FromSeconds(5.2);

        [TestMethod]
        public async Task ACardWearingASkinTransformsIntoItsLookAndKeepsIt()
        {
            var player = await Harness.AddPlayerAsync("Alice", configure: c => c.SpPoint = 10_000);
            await Harness.GiveAsync(player, new SpecialistInstance { ItemVNum = WorldData.ChefCard, Amount = 1, SpLevel = 1 },
                NoscorePocketType.Wear, (short)EquipmentType.Sp);
            await Harness.GiveAsync(player, WorldData.MasterchefSkin, 1, pocket: NoscorePocketType.Main);
            await using (var bob = await Harness.EnterAsync("Bob", x: 82, y: 116))
            await using (var alice = Harness.Client(player))
            {
                await alice.EnterWorldAsync(player.Slot);
                var used = alice.Mark();
                await alice.SendAsync(new UseItemPacket
                {
                    VisualType = VisualType.Player, VisualId = alice.CharacterId, Type = PocketType.Main, Slot = 0, Mode = 0, Parameter = 0
                });
                await alice.ExpectSinceAsync<IvnPacket>(used, p => p.Type == PocketType.Main
                    && p.IvnSubPackets?.Single() is { Slot: 0, VNum: 0 or -1 }, "the skin used up");

                await alice.SendAsync(new SpTransformPacket { Type = SlPacketType.WearSp });
                await alice.ExpectAsync<DelayPacket>();
                await Task.Delay(TransformingTime);
                var seen = bob.Mark();
                await alice.SendAsync(new SpTransformPacket { Type = SlPacketType.WearSpAndTransform });

                await bob.ExpectSinceAsync<CModePacket>(seen, p => p.VisualType == VisualType.Player && p.VisualId == alice.CharacterId
                    && p.Morph == WorldData.MasterchefLook, "Alice in the Masterchef look");
            }

            await using var back = await Harness.ComeBackAsync(player);
            await using var context = Harness.Database.CreateContext();
            var card = await context.Set<SpecialistInstance>().AsNoTracking()
                .SingleAsync(i => i.CharacterId == player.CharacterId && i.ItemVNum == WorldData.ChefCard);
            Assert.AreEqual(WorldData.MasterchefLook, card.Skin);
        }
    }
}
