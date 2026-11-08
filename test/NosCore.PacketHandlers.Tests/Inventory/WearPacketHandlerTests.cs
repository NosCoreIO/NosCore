//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.MateService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class WearPacketHandlerTests
    {
        [TestMethod]
        public async Task WearingOnAPartnerGoesToThePartnersGear()
        {
            await TestHelpers.ResetAsync();
            var session = await TestHelpers.Instance.GenerateSessionAsync();
            var partners = new Mock<IPartnerEquipmentService>();

            await new WearPacketHandler(partners.Object)
                .ExecuteAsync(new WearPacket { InventorySlot = 4, PartnerSlot = 2 }, session);

            partners.Verify(s => s.WearAsync(It.IsAny<PlayerComponentBundle>(), 4, 2), Times.Once);
        }

        [TestMethod]
        public async Task RemovingFromAPartnerGoesToThePartnersGear()
        {
            await TestHelpers.ResetAsync();
            var session = await TestHelpers.Instance.GenerateSessionAsync();
            var partners = new Mock<IPartnerEquipmentService>();

            await new RemovePacketHandler(new Mock<NosCore.GameObject.Services.BattleService.IVitalityService>().Object,
                    new Mock<NosCore.GameObject.Services.BattleService.IBattleStatsProvider>().Object, partners.Object)
                .ExecuteAsync(new RemovePacket { InventorySlot = EquipmentType.Boots, Type = 1 }, session);

            partners.Verify(s => s.RemoveAsync(It.IsAny<PlayerComponentBundle>(), EquipmentType.Boots, 1), Times.Once);
        }
    }
}
