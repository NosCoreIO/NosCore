//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.DignityService;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.PacketHandlerService;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class RequestNpcPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private RequestNpcPacketHandler _requestNpcPacketHandler = null!;
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            // Register ShoppingPacketHandler so the shop-no-dialog branch can dispatch
            // a ShoppingPacket through HandlePacketsAsync and we can observe the n_inv.
            var shoppingHandler = new ShoppingPacketHandler(
                Logger,
                new Mock<IDignityService>().Object,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
            _session = await TestHelpers.Instance.GenerateSessionAsync(new List<IPacketHandler> { shoppingHandler });
            _requestNpcPacketHandler = new RequestNpcPacketHandler(
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task RequestNpcWithNonExistentNpcShouldNotThrow()
        {
            await new Spec("Request NPC with non-existent NPC should not throw")
                .WhenAsync(ExecutingRequestNpcPacketWithNonExistentNpc)
                .Then(SessionShouldRemainValid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestNpcWithUnknownVisualTypeShouldNotThrow()
        {
            await new Spec("Request NPC with unknown visual type should not throw")
                .WhenAsync(ExecutingRequestNpcPacketWithUnknownType)
                .Then(SessionShouldRemainValid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestNpcWithExistingNpcShouldNotThrow()
        {
            await new Spec("Request NPC with existing NPC should not throw")
                .Given(NpcWithDialogExistsOnMap)
                .WhenAsync(ExecutingRequestNpcPacketWithNpcType)
                .Then(SessionShouldRemainValid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestNpcOnShopWithNoDialogOpensShopDirectly()
        {
            // OpenNos parity: grocer/weapon NPCs have a Shop but Dialog=0 — clicking them
            // must open the shop tab directly instead of replying with npc_req ... 0 (a
            // no-op on the client). Regression from #271bad47 era.
            await new Spec("req_npc on a shop-only NPC opens the shop (n_inv) without a dialog round-trip")
                .Given(ShopOnlyNpcExistsOnMap)
                .WhenAsync(ExecutingRequestNpcPacketWithNpcType)
                .Then(NInvPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void NpcWithDialogExistsOnMap()
        {
            var npc = new MapNpcDto
            {
                MapNpcId = 100,
                Dialog = 100,
                MapId = _session.Character.MapInstance.Map.MapId,
                MapX = 1,
                MapY = 1,
                VNum = 1
            };
            _session.Character.MapInstance.LoadNpcs(
                new List<MapNpcDto> { npc },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1 } });
        }

        private void ShopOnlyNpcExistsOnMap()
        {
            var npc = new MapNpcDto
            {
                MapNpcId = 100,
                Dialog = 0,
                MapId = _session.Character.MapInstance.Map.MapId,
                MapX = 1,
                MapY = 1,
                VNum = 1
            };
            _session.Character.MapInstance.LoadNpcs(
                new List<MapNpcDto> { npc },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1 } });
            var bundle = _session.Character.MapInstance.GetNpcById(100)!.Value;
            bundle.InitializeShopAndDialog(
                new ShopDto { ShopId = 1, MapNpcId = 100, MenuType = 0, ShopType = 0 },
                null,
                new List<ShopItemDto>
                {
                    new() { ShopItemId = 1, ShopId = 1, ItemVNum = 1012, Slot = 0, Type = 0 }
                },
                TestHelpers.Instance.GenerateItemProvider());
        }

        private void NInvPacketShouldBeSent()
        {
            // ShoppingPacketHandler emits the n_inv; if the dialog-first branch fires
            // instead we won't see it on the session.
            Assert.IsTrue(_session.LastPackets.Any(p => p is NInvPacket),
                "expected an n_inv packet after clicking a shop-only NPC");
        }

        private async Task ExecutingRequestNpcPacketWithNpcType()
        {
            var packet = new RequestNpcPacket
            {
                Type = VisualType.Npc,
                TargetId = 100
            };
            await _requestNpcPacketHandler.ExecuteAsync(packet, _session);
        }

        private async Task ExecutingRequestNpcPacketWithNonExistentNpc()
        {
            var packet = new RequestNpcPacket
            {
                Type = VisualType.Npc,
                TargetId = 99999
            };
            await _requestNpcPacketHandler.ExecuteAsync(packet, _session);
        }

        private async Task ExecutingRequestNpcPacketWithUnknownType()
        {
            var packet = new RequestNpcPacket
            {
                Type = VisualType.Monster,
                TargetId = 1
            };
            await _requestNpcPacketHandler.ExecuteAsync(packet, _session);
        }

        private void SessionShouldRemainValid()
        {
            Assert.IsNotNull(_session);
        }
    }
}
