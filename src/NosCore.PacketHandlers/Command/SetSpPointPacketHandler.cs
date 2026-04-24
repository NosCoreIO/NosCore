//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;

namespace NosCore.PacketHandlers.Command
{
    public class SetSpPointPacketHandler(IOptions<WorldConfiguration> worldConfiguration)
        : PacketHandler<SetSpPointPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(SetSpPointPacket packet, ClientSession session)
        {
            var clamped = packet.SpPoint < 0 ? 0
                : packet.SpPoint > worldConfiguration.Value.MaxSpPoints ? worldConfiguration.Value.MaxSpPoints
                : packet.SpPoint;
            session.Character.SpPoint = clamped;
            return session.SendPacketAsync(session.Character.GenerateSpPoint(worldConfiguration));
        }
    }
}
