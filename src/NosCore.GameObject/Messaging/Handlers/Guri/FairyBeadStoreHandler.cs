//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.FairyBeadService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // The fairy bead window's confirmation: guri 209 0 <bead slot> <pocket> <fairy slot>.
    [UsedImplicitly]
    public sealed class FairyBeadStoreHandler(IFairyBeadService fairyBeadService)
    {
        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            var packet = evt.Packet;
            if (packet.Type != GuriPacketType.StoreFairyInPearl || packet.Argument != 0
                || packet.VisualId is not (>= 0 and <= short.MaxValue and var beadSlot)
                || packet.Data is not (>= 0 and <= byte.MaxValue and var pocket)
                || !short.TryParse(packet.Value, out var fairySlot))
            {
                return;
            }

            var session = evt.ClientSession;
            if (fairyBeadService.Store(session.Character, (short)beadSlot, ((PocketType)pocket).ToNoscorePocketType(), fairySlot)
                is not { } stored)
            {
                return;
            }

            await session.SendPacketAsync(stored.Remaining.GeneratePocketChange(stored.Fairy.Type.ToPocketType(), stored.Fairy.Slot));
            await session.SendPacketAsync(stored.Bead.GeneratePocketChange(stored.Bead.Type.ToPocketType(), stored.Bead.Slot));
            await session.SendPacketAsync(new GuriPacket { Type = GuriPacketType.CloseFairyPearlWindow });
        }
    }
}
