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
    public class SetSpAdditionPointPacketHandler(IOptions<WorldConfiguration> worldConfiguration)
        : PacketHandler<SetSpAdditionPointPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(SetSpAdditionPointPacket packet, ClientSession session)
        {
            var clamped = packet.SpAdditionPoint < 0 ? 0
                : packet.SpAdditionPoint > worldConfiguration.Value.MaxAdditionalSpPoints ? worldConfiguration.Value.MaxAdditionalSpPoints
                : packet.SpAdditionPoint;
            session.Character.SpAdditionPoint = clamped;
            return session.SendPacketAsync(session.Character.GenerateSpPoint(worldConfiguration));
        }
    }
}
