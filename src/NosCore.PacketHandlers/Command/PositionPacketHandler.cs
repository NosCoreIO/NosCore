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
    public class PositionPacketHandler : PacketHandler<PositionPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(PositionPacket _, ClientSession session)
        {
            return session.SendPacketAsync(session.Character.GenerateSay(
                $"Map:{session.Character.MapInstance.Map.MapId} - X:{session.Character.PositionX} - Y:{session.Character.PositionY} - " +
                $"Dir:{session.Character.Direction} - Cell:{session.Character.MapInstance.Map[session.Character.PositionX, session.Character.PositionY]}",
                SayColorType.Green));
        }
    }
}
