//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Quicklist;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quicklist;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class QSetPacketHandler : PacketHandler<QsetPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(QsetPacket qSetPacket, ClientSession session)
        {
            short data1 = qSetPacket.FirstData ?? 0;
            short data2 = qSetPacket.SecondData ?? 0;
            var index = qSetPacket.OriginQuickList;
            var slot = qSetPacket.OriginQuickListSlot;

            IReadOnlyList<QsetClientPacket> packets = qSetPacket.Type switch
            {
                QSetType.Default or QSetType.Set =>
                    [session.Character.SetQuicklistEntry(qSetPacket.Type, index, slot, data1, data2)],
                QSetType.Move => session.Character.MoveQuicklistEntry(data1, data2, index, slot),
                QSetType.Remove => [session.Character.RemoveQuicklistEntry(index, slot)],
                _ => [],
            };
            foreach (var packet in packets)
            {
                await session.SendPacketAsync(packet);
            }
        }
    }
}
