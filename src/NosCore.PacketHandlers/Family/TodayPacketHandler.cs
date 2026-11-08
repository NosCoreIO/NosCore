//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.ServerPackets.Families;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class TodayPacketHandler : PacketHandler<TodayPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(TodayPacket packet, ClientSession session) =>
            session.Character.Family == null ? Task.CompletedTask : session.SendPacketAsync(new TodayStcPacket());
    }
}
