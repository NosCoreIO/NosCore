//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class SpeedPacketHandler : PacketHandler<SpeedPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(SpeedPacket speedPacket, ClientSession session)
        {
            if ((speedPacket.Speed <= 0) || (speedPacket.Speed >= 60))
            {
                return session.SendPacketAsync(session.Character.GenerateSay(speedPacket.Help(), SayColorType.Yellow));
            }

            session.Character.VehicleSpeed = speedPacket.Speed >= 60 ? (byte)59 : speedPacket.Speed;
            return session.SendPacketAsync(session.Character.GenerateCond());
        }
    }
}
