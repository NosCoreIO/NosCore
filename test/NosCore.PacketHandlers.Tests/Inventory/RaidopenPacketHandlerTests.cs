//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.InventoryService;
using NosCore.PacketHandlers.Generated;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class RaidopenPacketHandlerTests : SpecBase
    {
        private RaidopenPacketHandler Handler;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            Handler = World.Create<RaidopenPacketHandler>();
        }

        [TestMethod]
        public async Task OpeningTheRaidWindowListsTheRaidPocket()
        {
            await new Spec("Opening the raid window lists what the raid pocket holds, three fields per item")
                .Given(TheRaidPocketHolds_Of_, 4, 1012)
                .WhenAsync(OpeningTheRaidWindow)
                .Then(TheOnlyAnswerShouldBe_, new InvPacket
                {
                    Type = PocketType.Raid,
                    IvnSubPackets = new List<IvnSubPacket>
                    {
                        new() { Slot = 0, VNum = 1012, RareAmount = 4, UpgradeDesign = null, SecondUpgrade = null, Unknown5 = null, Unknown6 = null }
                    }
                })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEmptyRaidPocketIsListedEmpty()
        {
            await new Spec("Opening the raid window with nothing in the raid pocket sends the empty list")
                .WhenAsync(OpeningTheRaidWindow)
                .Then(TheOnlyAnswerShouldBe_, new InvPacket { Type = PocketType.Raid, IvnSubPackets = new List<IvnSubPacket>() })
                .ExecuteAsync();
        }

        [TestMethod]
        public void TheWorldReadsTheLineAsTheRaidWindowPacket()
        {
            Assert.IsInstanceOfType<RaidopenPacket>(new Deserializer(PacketTypes.World()).Deserialize("raidopen"));
            Assert.IsTrue(GeneratedPacketHandlers.WorldHandlerTypes.Contains(typeof(RaidopenPacketHandler)));
        }

        private void TheRaidPocketHolds_Of_(int amount, int vnum) =>
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create((short)vnum, (short)amount), Session.Character.CharacterId),
                NoscorePocketType.Raid, 0);

        private async Task OpeningTheRaidWindow()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new RaidopenPacket(), Session);
        }

        private void TheOnlyAnswerShouldBe_(InvPacket expected) =>
            PacketAssert.AreEqual(expected, Session.LastPackets.OfType<InvPacket>().Single());
    }
}
